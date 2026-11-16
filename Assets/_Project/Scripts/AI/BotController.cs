using SwordArt.Audio;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Player;
using SwordArt.Tournament;
using SwordArt.UI;
using SwordArt.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace SwordArt.AI
{
    /// <summary>
    /// Tournament bot driven by a state machine:
    /// Idle → Patrol → Detect → Chase → Attack ⇄ TakeCover → Reload → Dead.
    /// Uses NavMesh for movement, line-of-sight raycasts (no seeing through walls), hearing of
    /// gunshots, distance management, cover seeking, the GGO "bullet line" before firing, and
    /// stays inside the shrinking safe zone.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(Participant))]
    public sealed class BotController : MonoBehaviour
    {
        [SerializeField] private BotProfile profile;
        [SerializeField] private Transform eye;
        [SerializeField] private Transform weaponMount;
        [SerializeField] private ProceduralCharacterAnimator characterAnimator;
        [SerializeField] private LineRenderer bulletLine;
        [SerializeField] private WorldNameTag nameTag;
        [Tooltip("Layers that block sight and bullets (environment + characters).")]
        [SerializeField] private LayerMask sightMask = ~0;

        private const float BotVersusBotDamage = 0.5f;

        private readonly StateMachine _fsm = new StateMachine();
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private NavMeshAgent _agent;
        private Health _health;
        private Participant _self;
        private TournamentManager _tournament;
        private Transform _muzzle;

        private IdleState _idle;
        private PatrolState _patrol;
        private DetectState _detect;
        private ChaseState _chase;
        private AttackState _attack;
        private CoverState _cover;
        private ReloadState _reload;
        private DeadState _dead;

        private Participant _target;
        private bool _targetVisible;
        private Vector3 _lastKnownPosition;
        private float _lastSeenTime = -100f;
        private float _perceptionTimer;
        private int _ammo;
        private float _outsideZoneTimer;
        private float _lastCoverTime = -100f;
        private bool _active;

        /// <summary>Name of the current state (debug / tests).</summary>
        public string StateName => _fsm.Current?.GetType().Name.Replace("State", "") ?? "None";
        public BotProfile Profile => profile;
        public Participant Self => _self;
        public Participant Target => _target;
        public bool TargetVisible => _targetVisible;
        public int Ammo => _ammo;

        private WeaponData Weapon => profile != null ? profile.weapon : null;
        private Vector3 EyePosition => eye != null ? eye.position : transform.position + Vector3.up * 1.6f;

        /// <summary>Configures references (used by the prefab builder).</summary>
        public void ConfigureReferences(Transform eyeTransform, Transform mount, ProceduralCharacterAnimator anim, LineRenderer line, WorldNameTag tag, LayerMask mask)
        {
            eye = eyeTransform;
            weaponMount = mount;
            characterAnimator = anim;
            bulletLine = line;
            nameTag = tag;
            sightMask = mask;
        }

        /// <summary>Assigns profile, name and tournament; called by the spawner before the match.</summary>
        public void Initialize(TournamentManager tournament, BotProfile botProfile, string botName)
        {
            _tournament = tournament;
            profile = botProfile;
            _self.Configure(botName, false);
            _health.SetMax(profile.maxHealth, true);
            _ammo = Weapon != null ? Weapon.magazineSize : 30;
            _agent.speed = profile.walkSpeed;
            if (nameTag != null) nameTag.Configure(botName, profile.archetype, new Color(1f, 0.75f, 0.45f), 2.15f, 18f);
            CreateWeaponModel();
            ApplyOutfit();
        }

        /// <summary>Allows the bot to act (match start).</summary>
        public void Activate()
        {
            _active = true;
            _fsm.ChangeState(_patrol);
        }

        /// <summary>Freezes the bot (match end).</summary>
        public void Deactivate()
        {
            _active = false;
            if (_health.IsAlive) _fsm.ChangeState(_idle);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _self = GetComponent<Participant>();
            _idle = new IdleState(this);
            _patrol = new PatrolState(this);
            _detect = new DetectState(this);
            _chase = new ChaseState(this);
            _attack = new AttackState(this);
            _cover = new CoverState(this);
            _reload = new ReloadState(this);
            _dead = new DeadState(this);
            _health.Damaged += OnDamaged;
            _health.Died += _ => _fsm.ChangeState(_dead);
        }

        private void OnEnable() => Hitscan.ShotFired += OnShotHeard;
        private void OnDisable() => Hitscan.ShotFired -= OnShotHeard;

        private void Start()
        {
            if (_fsm.Current == null) _fsm.ChangeState(_idle);
            if (bulletLine != null) bulletLine.enabled = false;
        }

        private void Update()
        {
            if (_fsm.Current == _dead) return;
            if (_active)
            {
                _perceptionTimer -= Time.deltaTime;
                if (_perceptionTimer <= 0f)
                {
                    _perceptionTimer = 0.15f;
                    Perceive();
                }
                TrackZone();
            }
            _fsm.Tick(Time.deltaTime);
            if (characterAnimator != null) characterAnimator.SpeedOverride = _agent.enabled ? _agent.velocity.magnitude : 0f;
        }

        // ---------------- Perception ----------------

        private void Perceive()
        {
            if (_tournament == null || profile == null) return;
            Participant best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in _tournament.Participants)
            {
                if (p == null || p == _self || !p.IsAlive) continue;
                Vector3 to = p.ChestPosition - EyePosition;
                float d = to.magnitude;
                if (d > profile.sightRange) continue;
                bool inFov = Vector3.Angle(transform.forward, to) <= profile.fieldOfView * 0.5f || d < 8f || p == _target;
                if (!inFov || !HasLineOfSight(p)) continue;
                // Slight preference for the current target to avoid flip-flopping.
                if (p == _target) d *= 0.7f;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }

            _targetVisible = best != null;
            if (best != null)
            {
                _target = best;
                _lastKnownPosition = best.transform.position;
                _lastSeenTime = Time.time;
            }
            else if (_target != null && (!_target.IsAlive || Time.time - _lastSeenTime > 8f))
            {
                _target = null;
            }
        }

        /// <summary>True if nothing solid is between the bot's eyes and the participant's chest/head.</summary>
        public bool HasLineOfSight(Participant p)
        {
            return RayReaches(EyePosition, p.ChestPosition, p.transform) || RayReaches(EyePosition, p.ChestPosition + Vector3.up * 0.4f, p.transform);
        }

        private bool RayReaches(Vector3 from, Vector3 to, Transform target)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            int count = Physics.RaycastNonAlloc(from, dir / dist, _hits, dist + 0.3f, sightMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            Transform nearestT = null;
            for (int i = 0; i < count; i++)
            {
                var h = _hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance < nearest)
                {
                    nearest = h.distance;
                    nearestT = h.collider.transform;
                }
            }
            return nearestT == null || nearestT.IsChildOf(target);
        }

        private void OnShotHeard(Vector3 origin, GameObject shooter)
        {
            if (!_active || _fsm.Current == _dead || shooter == gameObject || profile == null) return;
            if (_target != null && Time.time - _lastSeenTime < 3f) return;
            if (Vector3.Distance(origin, transform.position) > profile.hearingRange) return;
            var source = shooter != null ? shooter.GetComponentInParent<Participant>() : null;
            if (source != null && source != _self) _target = source;
            _lastKnownPosition = origin;
            _lastSeenTime = Time.time - 2f;
            if (_fsm.Current == _patrol || _fsm.Current == _idle) _fsm.ChangeState(_chase);
        }

        private void OnDamaged(DamageInfo info)
        {
            if (!_active || !_health.IsAlive) return;
            var attacker = info.Source != null ? info.Source.GetComponentInParent<Participant>() : null;
            if (attacker != null && attacker != _self)
            {
                _target = attacker;
                _lastKnownPosition = attacker.transform.position;
                _lastSeenTime = Time.time - 1f;
                if (_fsm.Current == _patrol || _fsm.Current == _idle) _fsm.ChangeState(_detect);
            }
            if (_health.Normalized < profile.coverHealthThreshold && Time.time - _lastCoverTime > 10f &&
                _fsm.Current != _cover && _fsm.Current != _reload && attacker != null)
                _fsm.ChangeState(_cover);
        }

        private void TrackZone()
        {
            var zone = _tournament != null ? _tournament.Zone : null;
            if (zone == null || !zone.IsRunning) return;
            _outsideZoneTimer = zone.IsInside(transform.position) ? 0f : _outsideZoneTimer + Time.deltaTime;
        }

        private bool MustReturnToZone => _outsideZoneTimer > 5f;

        // ---------------- Actions ----------------

        private bool AgentReady => _agent.enabled && _agent.isOnNavMesh;

        private bool MoveTo(Vector3 position, bool run)
        {
            if (!AgentReady) return false;
            if (!NavMesh.SamplePosition(position, out var hit, 10f, NavMesh.AllAreas)) return false;
            _agent.isStopped = false;
            _agent.speed = run ? profile.runSpeed : profile.walkSpeed;
            return _agent.SetDestination(hit.position);
        }

        private void StopMoving()
        {
            if (AgentReady) _agent.isStopped = true;
        }

        private bool Arrived => AgentReady && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.4f;

        private void FaceTowards(Vector3 point, float degreesPerSecond)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), degreesPerSecond * Time.deltaTime);
        }

        private Vector3 AimPoint => _target != null ? _target.ChestPosition : _lastKnownPosition + Vector3.up * 1.3f;

        private void ShowBulletLine(bool show)
        {
            if (bulletLine == null) return;
            bulletLine.enabled = show;
            if (!show) return;
            Vector3 from = _muzzle != null ? _muzzle.position : EyePosition;
            Vector3 to = AimPoint + Random.insideUnitSphere * 0.15f;
            Vector3 dir = (to - from).normalized;
            bulletLine.SetPosition(0, from);
            bulletLine.SetPosition(1, from + dir * Mathf.Min(Weapon != null ? Weapon.range : 80f, Vector3.Distance(from, to) + 3f));
        }

        private bool CanShootTarget()
        {
            if (_target == null || !_target.IsAlive || !_targetVisible || Weapon == null) return false;
            float d = Vector3.Distance(EyePosition, _target.ChestPosition);
            if (d > Weapon.range) return false;
            Vector3 flatDir = _target.transform.position - transform.position;
            flatDir.y = 0f;
            return Vector3.Angle(transform.forward, flatDir) < 20f;
        }

        private void Shoot()
        {
            var weapon = Weapon;
            _ammo--;
            Vector3 origin = EyePosition;
            Vector3 aim = AimPoint + Vector3.up * Random.Range(-0.35f, 0.45f);
            // Bots shoot "half-aimed": between ADS and hip spread, scaled by skill.
            float spread = Mathf.Lerp(weapon.aimSpread, weapon.spread, 0.35f) * profile.spreadMultiplier;
            // Bot-vs-bot fights are slowed down so matches last long enough for the player to matter.
            float damage = weapon.damage * profile.damageMultiplier * (_target != null && !_target.IsPlayer ? BotVersusBotDamage : 1f);
            var result = Hitscan.Fire(origin, Hitscan.ApplySpread((aim - origin).normalized, spread), weapon.range,
                damage, gameObject, sightMask, weapon.headshotMultiplier);
            Vector3 muzzle = _muzzle != null ? _muzzle.position : origin;
            CombatFX.MuzzleFlash(muzzle, transform.forward, weapon.tracerColor);
            CombatFX.Tracer(muzzle, result.Point, weapon.tracerColor);
            var sfx = weapon.weaponType == WeaponType.Pistol ? Sfx.GunshotPistol : weapon.weaponType == WeaponType.SniperRifle ? Sfx.GunshotSniper : Sfx.GunshotRifle;
            GameManager.Instance?.Audio?.PlayAt(sfx, muzzle, 0.9f, weapon.soundPitch, 150f);
        }

        private void CreateWeaponModel()
        {
            if (weaponMount == null || Weapon == null) return;
            for (int i = weaponMount.childCount - 1; i >= 0; i--) Destroy(weaponMount.GetChild(i).gameObject);
            var model = Weapon.modelPrefab != null ? Instantiate(Weapon.modelPrefab, weaponMount) : WeaponPlaceholder.Create(Weapon, weaponMount);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            _muzzle = new GameObject("Muzzle").transform;
            _muzzle.SetParent(model.transform, false);
            _muzzle.localPosition = Weapon.muzzleLocalPosition;
        }

        private void ApplyOutfit()
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r == bulletLine || (weaponMount != null && r.transform.IsChildOf(weaponMount))) continue;
                string n = r.sharedMaterial != null ? r.sharedMaterial.name : "";
                Color? c = n.Contains("Shirt") || n.Contains("Outfit") ? profile.outfitColor
                    : n.Contains("Pants") ? profile.outfitColor * 0.7f
                    : n.Contains("Accent") ? profile.accentColor
                    : (Color?)null;
                if (c == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c.Value);
                r.SetPropertyBlock(block);
            }
        }

        // ---------------- States ----------------

        private abstract class BotState : IState
        {
            protected readonly BotController Bot;
            protected BotState(BotController bot) => Bot = bot;
            protected StateMachine Fsm => Bot._fsm;
            public virtual void Enter() { }
            public virtual void Tick(float dt) { }
            public virtual void Exit() { }
        }

        private sealed class IdleState : BotState
        {
            public IdleState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot.StopMoving();
                Bot.ShowBulletLine(false);
            }

            public override void Tick(float dt)
            {
                if (Bot._active && Fsm.TimeInState > 0.5f) Fsm.ChangeState(Bot._patrol);
            }
        }

        private sealed class PatrolState : BotState
        {
            private float _wait;
            private float _stuck;

            public PatrolState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot._agent.updateRotation = true;
                Bot.ShowBulletLine(false);
                PickDestination();
            }

            private void PickDestination()
            {
                var zone = Bot._tournament != null ? Bot._tournament.Zone : null;
                Vector3 p = zone != null && zone.IsRunning
                    ? zone.RandomPointInside(6f, true)
                    : Bot.transform.position + new Vector3(Random.Range(-30f, 30f), 0f, Random.Range(-30f, 30f));
                Bot.MoveTo(p, Bot.MustReturnToZone);
                _wait = Random.Range(0.5f, 2.5f);
                _stuck = 0f;
            }

            public override void Tick(float dt)
            {
                if (!Bot.MustReturnToZone && Bot._target != null && Bot._targetVisible)
                {
                    Fsm.ChangeState(Bot._detect);
                    return;
                }
                if (Bot.Arrived)
                {
                    _wait -= dt;
                    if (_wait <= 0f) PickDestination();
                }
                else if (Bot._agent.velocity.sqrMagnitude < 0.05f)
                {
                    _stuck += dt;
                    if (_stuck > 3f) PickDestination();
                }
            }
        }

        private sealed class DetectState : BotState
        {
            public DetectState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot.StopMoving();
                Bot._agent.updateRotation = false;
            }

            public override void Tick(float dt)
            {
                Bot.FaceTowards(Bot._lastKnownPosition, 360f);
                if (Fsm.TimeInState < Bot.profile.reactionTime) return;
                Fsm.ChangeState(Bot._targetVisible ? (IState)Bot._attack : Bot._chase);
            }
        }

        private sealed class ChaseState : BotState
        {
            private float _repath;

            public ChaseState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot._agent.updateRotation = true;
                Bot.ShowBulletLine(false);
                Bot.MoveTo(Bot._lastKnownPosition, true);
                _repath = 0.5f;
            }

            public override void Tick(float dt)
            {
                if (Bot._targetVisible && Bot._target != null)
                {
                    float d = Vector3.Distance(Bot.transform.position, Bot._target.transform.position);
                    if (d <= Bot.profile.preferredMaxDistance || d <= Bot.Weapon.range * 0.6f)
                    {
                        Fsm.ChangeState(Bot._attack);
                        return;
                    }
                }
                _repath -= dt;
                if (_repath <= 0f)
                {
                    _repath = 0.6f;
                    Bot.MoveTo(Bot._lastKnownPosition, true);
                }
                if ((Bot.Arrived && !Bot._targetVisible) || Fsm.TimeInState > 20f || Bot.MustReturnToZone)
                    Fsm.ChangeState(Bot._patrol);
            }
        }

        private sealed class AttackState : BotState
        {
            private float _aim;
            private int _burstLeft;
            private float _nextShot;
            private float _pauseUntil;
            private float _strafeTimer;
            private float _lostSight;

            public AttackState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot._agent.updateRotation = false;
                _aim = 0f;
                _burstLeft = Random.Range(Bot.profile.burstSize.x, Bot.profile.burstSize.y + 1);
                _strafeTimer = 0f;
                _lostSight = 0f;
            }

            public override void Exit() => Bot.ShowBulletLine(false);

            public override void Tick(float dt)
            {
                var bot = Bot;
                var target = bot._target;
                if (target == null || !target.IsAlive)
                {
                    Fsm.ChangeState(bot._patrol);
                    return;
                }
                if (bot.MustReturnToZone && bot._outsideZoneTimer > 8f)
                {
                    Fsm.ChangeState(bot._patrol);
                    return;
                }

                bot.FaceTowards(target.transform.position, 300f);
                _lostSight = bot._targetVisible ? 0f : _lostSight + dt;
                if (_lostSight > 1.2f)
                {
                    Fsm.ChangeState(bot._chase);
                    return;
                }

                ManageDistance(dt, target);

                if (bot._ammo <= 0)
                {
                    Fsm.ChangeState(bot._reload);
                    return;
                }

                if (Time.time < _pauseUntil || !bot.CanShootTarget())
                {
                    bot.ShowBulletLine(false);
                    _aim = Mathf.Max(0f, _aim - dt);
                    return;
                }

                if (_aim < bot.profile.aimTime)
                {
                    _aim += dt;
                    bot.ShowBulletLine(true);
                    return;
                }

                bot.ShowBulletLine(true);
                if (Time.time >= _nextShot)
                {
                    bot.Shoot();
                    _nextShot = Time.time + bot.Weapon.FireInterval * 1.2f;
                    _burstLeft--;
                    if (_burstLeft <= 0)
                    {
                        _burstLeft = Random.Range(bot.profile.burstSize.x, bot.profile.burstSize.y + 1);
                        _pauseUntil = Time.time + Random.Range(bot.profile.burstPause.x, bot.profile.burstPause.y);
                        _aim = bot.profile.aimTime * 0.4f;
                    }
                }
            }

            private void ManageDistance(float dt, Participant target)
            {
                var bot = Bot;
                _strafeTimer -= dt;
                if (_strafeTimer > 0f) return;
                _strafeTimer = Random.Range(1.2f, 2.8f);

                Vector3 toTarget = target.transform.position - bot.transform.position;
                float d = toTarget.magnitude;
                Vector3 dir = toTarget / Mathf.Max(d, 0.01f);
                Vector3 side = Vector3.Cross(Vector3.up, dir) * (Random.value > 0.5f ? 1f : -1f);
                Vector3 destination;
                if (d > bot.profile.preferredMaxDistance) destination = bot.transform.position + dir * 6f + side * 2f;
                else if (d < bot.profile.preferredMinDistance) destination = bot.transform.position - dir * 5f + side * 2f;
                else destination = bot.transform.position + side * Random.Range(2f, 5f);
                bot.MoveTo(destination, false);
            }
        }

        private sealed class CoverState : BotState
        {
            private CoverPoint _point;

            public CoverState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot._lastCoverTime = Time.time;
                Bot._agent.updateRotation = true;
                Bot.ShowBulletLine(false);
                _point = CoverPoint.FindBest(Bot.transform.position, Bot._lastKnownPosition + Vector3.up * 1.3f, 25f, Bot.sightMask, Bot);
                if (_point == null || !Bot.MoveTo(_point.transform.position, true))
                {
                    _point = null;
                    Fsm.ChangeState(Bot._ammo < Bot.Weapon.magazineSize ? (IState)Bot._reload : Bot._attack);
                    return;
                }
                _point.Occupant = Bot;
            }

            public override void Exit()
            {
                if (_point != null && _point.Occupant == (Object)Bot) _point.Occupant = null;
                _point = null;
            }

            public override void Tick(float dt)
            {
                if (_point == null) return;
                if (Bot.Arrived || Fsm.TimeInState > 8f)
                {
                    if (Bot._ammo < Bot.Weapon.magazineSize) Fsm.ChangeState(Bot._reload);
                    else if (Fsm.TimeInState > 2.5f) Fsm.ChangeState(Bot._targetVisible ? (IState)Bot._attack : Bot._chase);
                }
            }
        }

        private sealed class ReloadState : BotState
        {
            public ReloadState(BotController b) : base(b) { }

            public override void Enter()
            {
                Bot.ShowBulletLine(false);
                GameManager.Instance?.Audio?.PlayAt(Sfx.Reload, Bot.transform.position, 0.7f, 1f, 30f);
            }

            public override void Tick(float dt)
            {
                if (Bot._target != null) Bot.FaceTowards(Bot._lastKnownPosition, 180f);
                if (Fsm.TimeInState < Bot.Weapon.reloadTime) return;
                Bot._ammo = Bot.Weapon.magazineSize;
                if (Bot._target != null && Bot._target.IsAlive)
                    Fsm.ChangeState(Bot._targetVisible ? (IState)Bot._attack : Bot._chase);
                else Fsm.ChangeState(Bot._patrol);
            }
        }

        private sealed class DeadState : BotState
        {
            public DeadState(BotController b) : base(b) { }

            public override void Enter()
            {
                var bot = Bot;
                bot._active = false;
                bot.ShowBulletLine(false);
                if (bot.AgentReady) bot._agent.isStopped = true;
                bot._agent.enabled = false;
                foreach (var c in bot.GetComponentsInChildren<Collider>()) c.enabled = false;
                if (bot.characterAnimator != null)
                {
                    bot.characterAnimator.Dead = true;
                    bot.characterAnimator.SpeedOverride = 0f;
                }
                if (bot.nameTag != null) bot.nameTag.SetText("DEAD", new Color(1f, 0.25f, 0.2f));
                CombatFX.Shatter(bot.transform.position + Vector3.up * 1f, new Color(1f, 0.3f, 0.35f));
            }
        }
    }
}
