using System.Collections.Generic;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Tournament;
using SwordArt.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Gun Gale Online HUD: bullet-circle crosshair, hit markers, health/armour, ammo and weapon
    /// slots, alive counter and match timer, safe-zone minimap and warnings, kill feed, countdown,
    /// target info, damage vignette, scope overlay and the hold-Tab statistics panel.
    /// Built at runtime on the persistent HUD canvas and destroyed with the scene.
    /// </summary>
    public sealed class GGOHUD : MonoBehaviour
    {
        [SerializeField] private TournamentManager tournament;
        [Tooltip("Half size of the arena in metres (minimap scale).")]
        [SerializeField] private float mapRadius = 130f;
        [SerializeField] private Vector3 mapCenter = Vector3.zero;

        private readonly UITheme _t = UITheme.Virtual;
        private RectTransform _root;

        private RectTransform _circle;
        private Image _circleImage;
        private readonly Image[] _ticks = new Image[4];
        private RectTransform _hitMarker;
        private Image[] _hitBars;
        private float _hitTimer;
        private bool _hitKill;
        private Image _scope;

        private Image _healthFill;
        private Image _healthLag;
        private Text _healthText;
        private Text _armorText;
        private Text _medkitText;

        private Text _ammoText;
        private Text _reserveText;
        private Text _weaponName;
        private Text _slotsText;
        private Text _reloadText;

        private Text _aliveText;
        private Text _timerText;
        private Text _zoneText;
        private Text _centerText;
        private Text _subCenterText;
        private Text _targetText;
        private Image _targetFill;
        private RectTransform _targetRoot;
        private Image _damageVignette;
        private Image _zoneVignette;

        private RectTransform _minimap;
        private RectTransform _zoneRing;
        private RectTransform _nextRing;
        private RectTransform _playerDot;

        private RectTransform _feed;
        private readonly List<(GameObject go, float time)> _feedItems = new List<(GameObject, float)>();

        private CanvasGroup _stats;
        private Text _statsText;

        private float _centerTimer;
        private float _damageFlash;
        private float _lastHealth = -1f;
        private ResultWindow _result;

        /// <summary>Configures the HUD (used by the scene builder).</summary>
        public void Configure(TournamentManager manager, Vector3 center, float radius)
        {
            tournament = manager;
            mapCenter = center;
            mapRadius = radius;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null || tournament == null) return;
            _root = UIKit.Rect(gm.UI.HudCanvas.transform, "GGO_HUD");
            _root.Stretch();
            _root.SetAsFirstSibling();

            BuildCrosshair();
            BuildVitals();
            BuildAmmo();
            BuildTop();
            BuildMinimap();
            BuildFeed();
            BuildStats();

            _result = gm.UI.CreateWindow<ResultWindow>("ResultWindow");

            var player = tournament.Player;
            player.Weapons.HitConfirmed += OnHit;
            player.Health.Damaged += OnPlayerDamaged;
            tournament.StateChanged += OnStateChanged;
            tournament.CountdownTick += OnCountdown;
            tournament.ParticipantEliminated += OnEliminated;
            tournament.MatchFinished += OnMatchFinished;
            OnStateChanged(tournament.State);
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_result != null) Destroy(_result.gameObject);
        }

        // ---------------- Build ----------------

        private void BuildCrosshair()
        {
            _scope = UIKit.Image(_root, "Scope", Color.black, MakeScopeSprite(), false);
            _scope.rectTransform.Stretch();
            _scope.preserveAspect = false;
            _scope.gameObject.SetActive(false);
            var scopeH = UIKit.Image(_scope.transform, "H", new Color(0, 0, 0, 0.9f));
            scopeH.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 2));
            var scopeV = UIKit.Image(_scope.transform, "V", new Color(0, 0, 0, 0.9f));
            scopeV.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2, 1400));

            _circle = UIKit.Rect(_root, "BulletCircle");
            _circle.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            _circleImage = UIKit.Image(_circle, "Ring", new Color(0.3f, 1f, 0.6f, 0.75f), UIKit.Ring, false);
            _circleImage.rectTransform.Stretch();
            for (int i = 0; i < 4; i++)
            {
                _ticks[i] = UIKit.Image(_circle, "Tick" + i, new Color(0.3f, 1f, 0.6f, 0.9f));
                _ticks[i].rectTransform.sizeDelta = i % 2 == 0 ? new Vector2(2, 8) : new Vector2(8, 2);
            }
            var dot = UIKit.Image(_root, "Dot", new Color(0.3f, 1f, 0.6f, 0.9f), UIKit.Circle, false);
            dot.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));

            _hitMarker = UIKit.Rect(_root, "HitMarker");
            _hitMarker.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            _hitBars = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var bar = UIKit.Image(_hitMarker, "Bar" + i, Color.white);
                float a = 45f + 90f * i;
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                bar.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), dir * 12f, new Vector2(12, 3));
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, a);
                _hitBars[i] = bar;
            }
            _hitMarker.gameObject.SetActive(false);

            _damageVignette = UIKit.Image(_root, "DamageVignette", new Color(1f, 0.1f, 0.1f, 0f), MakeVignetteSprite(), false);
            _damageVignette.rectTransform.Stretch();
            _zoneVignette = UIKit.Image(_root, "ZoneVignette", new Color(0.4f, 0.2f, 1f, 0f), MakeVignetteSprite(), false);
            _zoneVignette.rectTransform.Stretch();
        }

        private void BuildVitals()
        {
            var panel = UIKit.Panel(_root, "Vitals", _t);
            panel.raycastTarget = false;
            panel.rectTransform.Place(Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(32, 32), new Vector2(420, 110));
            var label = UIKit.Text(panel.transform, "HP", "HP", 22, _t.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(60, 36));
            _healthText = UIKit.Text(panel.transform, "HPText", "100 / 100", 22, _t.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            _healthText.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -10), new Vector2(-80, 36));

            var bar = UIKit.Image(panel.transform, "Bar", new Color(1, 1, 1, 0.08f), UIKit.Rounded);
            bar.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(-36, 16));
            _healthLag = UIKit.Image(bar.transform, "Lag", new Color(1f, 0.85f, 0.5f, 0.6f), UIKit.Rounded);
            _healthLag.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            _healthFill = UIKit.Image(bar.transform, "Fill", _t.Positive, UIKit.Rounded);
            _healthFill.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            _armorText = UIKit.Text(panel.transform, "Armor", "", 18, _t.Secondary);
            _armorText.rectTransform.Place(new Vector2(0, 0), new Vector2(0.6f, 0), new Vector2(0, 0), new Vector2(18, 8), new Vector2(0, 30));
            _medkitText = UIKit.Text(panel.transform, "Medkits", "", 18, _t.TextDim, TextAnchor.MiddleRight);
            _medkitText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 8), new Vector2(0, 30));
        }

        private void BuildAmmo()
        {
            var panel = UIKit.Panel(_root, "Ammo", _t);
            panel.raycastTarget = false;
            panel.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 32), new Vector2(380, 150));
            _weaponName = UIKit.Text(panel.transform, "Weapon", "", 22, _t.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            _weaponName.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -8), new Vector2(-36, 32));
            _ammoText = UIKit.Text(panel.transform, "Mag", "30", 56, _t.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            _ammoText.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.62f, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -2), new Vector2(0, 64));
            _reserveText = UIKit.Text(panel.transform, "Reserve", "/ 120", 28, _t.TextDim, TextAnchor.MiddleLeft);
            _reserveText.rectTransform.Place(new Vector2(0.64f, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -8), new Vector2(0, 40));
            _slotsText = UIKit.Text(panel.transform, "Slots", "", 16, _t.TextDim, TextAnchor.MiddleRight);
            _slotsText.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 8), new Vector2(-36, 28));
            _reloadText = UIKit.Text(_root, "Reload", "", 24, _t.Warning, TextAnchor.MiddleCenter, FontStyle.Bold);
            _reloadText.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(500, 40));
        }

        private void BuildTop()
        {
            var panel = UIKit.Panel(_root, "TopBar", _t);
            panel.raycastTarget = false;
            panel.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(460, 64));
            _aliveText = UIKit.Text(panel.transform, "Alive", "", 26, _t.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _aliveText.rectTransform.Stretch();
            _aliveText.rectTransform.offsetMin = new Vector2(22, 0);
            _timerText = UIKit.Text(panel.transform, "Timer", "", 26, _t.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            _timerText.rectTransform.Stretch();
            _timerText.rectTransform.offsetMax = new Vector2(-22, 0);

            _zoneText = UIKit.Text(_root, "Zone", "", 22, _t.Secondary, TextAnchor.MiddleCenter, FontStyle.Bold);
            _zoneText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(900, 34));

            _centerText = UIKit.Text(_root, "Center", "", 96, _t.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _centerText.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1600, 130));
            var outline = _centerText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.7f);
            outline.effectDistance = new Vector2(3, -3);
            _subCenterText = UIKit.Text(_root, "SubCenter", "", 26, _t.Text, TextAnchor.MiddleCenter);
            _subCenterText.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(1400, 40));
            _subCenterText.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

            _targetRoot = UIKit.Rect(_root, "Target");
            _targetRoot.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(260, 40));
            _targetText = UIKit.Text(_targetRoot, "Name", "", 20, _t.Negative, TextAnchor.UpperCenter, FontStyle.Bold);
            _targetText.rectTransform.Stretch();
            var tbar = UIKit.Image(_targetRoot, "Bar", new Color(0, 0, 0, 0.5f));
            tbar.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 5));
            _targetFill = UIKit.Image(tbar.transform, "Fill", _t.Negative);
            _targetFill.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            _targetRoot.gameObject.SetActive(false);
        }

        private void BuildMinimap()
        {
            var bg = UIKit.Image(_root, "Minimap", new Color(0.05f, 0.04f, 0.02f, 0.75f), UIKit.Circle, false);
            bg.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -24), new Vector2(230, 230));
            _minimap = bg.rectTransform;
            var border = UIKit.Image(_minimap, "Border", _t.Border, UIKit.Ring, false);
            border.rectTransform.Stretch();
            var label = UIKit.Text(_minimap, "N", "N", 16, _t.TextDim, TextAnchor.UpperCenter, FontStyle.Bold);
            label.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 20));

            _nextRing = UIKit.Image(_minimap, "NextZone", new Color(1f, 1f, 1f, 0.7f), UIKit.Ring, false).rectTransform;
            _zoneRing = UIKit.Image(_minimap, "Zone", new Color(0.35f, 0.75f, 1f, 1f), UIKit.Ring, false).rectTransform;
            _playerDot = UIKit.Image(_minimap, "Player", _t.Accent, UIKit.Circle, false).rectTransform;
            _playerDot.sizeDelta = new Vector2(10, 10);
            var arrow = UIKit.Image(_playerDot, "Dir", _t.Accent);
            arrow.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), Vector2.zero, new Vector2(3, 12));
        }

        private void BuildFeed()
        {
            _feed = UIKit.Rect(_root, "KillFeed");
            _feed.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -270), new Vector2(460, 300));
            UIKit.VerticalLayout(_feed, 6, new RectOffset(0, 0, 0, 0), TextAnchor.UpperRight);
        }

        private void BuildStats()
        {
            var panel = UIKit.Panel(_root, "Stats", _t, true);
            panel.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 620));
            _stats = panel.gameObject.AddComponent<CanvasGroup>();
            _stats.alpha = 0f;
            _statsText = UIKit.Text(panel.transform, "Text", "", 22, _t.Text, TextAnchor.UpperLeft);
            _statsText.rectTransform.Stretch(30);
        }

        // ---------------- Events ----------------

        private void OnHit(bool killed, bool headshot)
        {
            _hitTimer = 0.25f;
            _hitKill = killed;
            foreach (var b in _hitBars) b.color = killed ? _t.Negative : headshot ? _t.Warning : Color.white;
        }

        private void OnPlayerDamaged(DamageInfo info)
        {
            _damageFlash = Mathf.Min(1f, _damageFlash + info.Amount / 30f + 0.2f);
        }

        private void OnStateChanged(MatchState state)
        {
            switch (state)
            {
                case MatchState.Lobby:
                    ShowCenter("ЛОББИ BULLET OF BULLETS", "Подойдите к терминалу регистрации, чтобы начать турнир · можно потренироваться на мишенях", 9999f, 44);
                    break;
                case MatchState.InProgress:
                    ShowCenter("В БОЙ!", "Останьтесь последним выжившим", 2f, 96);
                    break;
            }
        }

        private void OnCountdown(int seconds)
        {
            if (seconds > 0) ShowCenter(seconds.ToString(), "Матч начнётся через...", 1.1f, 140);
        }

        private void OnEliminated(Participant victim, Participant killer)
        {
            string text = killer != null
                ? $"<color=#{Hex(killer.IsPlayer ? _t.Accent : _t.Text)}>{killer.DisplayName}</color>  ✕  <color=#{Hex(victim.IsPlayer ? _t.Accent : _t.TextDim)}>{victim.DisplayName}</color>"
                : $"<color=#{Hex(_t.Secondary)}>Зона</color>  ✕  {victim.DisplayName}";
            var bg = UIKit.Panel(_feed, "Kill", _t);
            bg.raycastTarget = false;
            UIKit.Height(bg, 40);
            var t = UIKit.Text(bg.transform, "Text", text, 20, _t.Text, TextAnchor.MiddleRight);
            t.rectTransform.Stretch();
            t.rectTransform.offsetMax = new Vector2(-14, 0);
            _feedItems.Add((bg.gameObject, Time.time));
            if (_feedItems.Count > 6)
            {
                Destroy(_feedItems[0].go);
                _feedItems.RemoveAt(0);
            }
            if (killer != null && killer.IsPlayer)
                ShowCenter("", $"Вы устранили {victim.DisplayName}", 2f, 40);
        }

        private void OnMatchFinished(MatchResult result) => _result.Show(result, tournament);

        private void ShowCenter(string text, string sub, float duration, int size)
        {
            _centerText.text = text;
            _centerText.fontSize = size;
            _subCenterText.text = sub;
            _centerTimer = duration;
        }

        // ---------------- Update ----------------

        private void Update()
        {
            if (_root == null || tournament == null) return;
            var player = tournament.Player;
            var weapons = player.Weapons;
            var gm = GameManager.Instance;

            // Crosshair / bullet circle.
            var cam = player.Motor.Camera;
            bool scoped = weapons.IsScoped;
            _scope.gameObject.SetActive(scoped);
            float spread = weapons.CurrentSpread;
            float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float canvasHeight = ((RectTransform)gm.UI.HudCanvas.transform).rect.height;
            float radius = Mathf.Tan(spread * Mathf.Deg2Rad) / Mathf.Tan(halfFov) * canvasHeight * 0.5f;
            radius = Mathf.Clamp(radius, 6f, 300f);
            _circle.sizeDelta = Vector2.Lerp(_circle.sizeDelta, Vector2.one * radius * 2f, Time.deltaTime * 20f);
            float r = _circle.sizeDelta.x * 0.5f;
            _ticks[0].rectTransform.anchoredPosition = new Vector2(0, r + 6);
            _ticks[1].rectTransform.anchoredPosition = new Vector2(r + 6, 0);
            _ticks[2].rectTransform.anchoredPosition = new Vector2(0, -r - 6);
            _ticks[3].rectTransform.anchoredPosition = new Vector2(-r - 6, 0);
            foreach (var tick in _ticks) tick.rectTransform.anchorMin = tick.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _circle.gameObject.SetActive(!scoped && player.Health.IsAlive);

            _hitTimer -= Time.deltaTime;
            _hitMarker.gameObject.SetActive(_hitTimer > 0f);
            _hitMarker.localScale = Vector3.one * (_hitKill ? 1.5f : 1f) * (1f + Mathf.Max(0f, _hitTimer) * 1.5f);

            // Vitals.
            var health = player.Health;
            float hp = health.Normalized;
            _healthFill.rectTransform.anchorMax = new Vector2(hp, 1f);
            float lag = Mathf.MoveTowards(_healthLag.rectTransform.anchorMax.x, hp, Time.deltaTime * 0.4f);
            _healthLag.rectTransform.anchorMax = new Vector2(Mathf.Max(lag, hp), 1f);
            _healthFill.color = hp > 0.5f ? _t.Positive : hp > 0.25f ? _t.Warning : _t.Negative;
            _healthText.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
            _armorText.text = player.Armor != null ? $"БРОНЯ: {player.Armor.displayName} (−{player.Armor.damageReduction * 100:0}%)" : "БРОНЯ: нет";
            _medkitText.text = $"[H] Аптечки: {player.MedkitCount}";
            if (_lastHealth >= 0f && health.Current > _lastHealth) _damageFlash = 0f;
            _lastHealth = health.Current;

            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, Time.deltaTime * 1.5f);
            float lowHealth = hp < 0.3f && health.IsAlive ? (0.25f + 0.15f * Mathf.Sin(Time.time * 6f)) : 0f;
            _damageVignette.color = new Color(1f, 0.1f, 0.1f, Mathf.Max(_damageFlash * 0.7f, lowHealth));

            // Ammo.
            var slot = weapons.Current;
            if (slot != null)
            {
                _weaponName.text = slot.Data.displayName.ToUpperInvariant();
                _ammoText.text = slot.Ammo.ToString();
                _ammoText.color = slot.Ammo == 0 ? _t.Negative : slot.Ammo <= slot.Data.magazineSize / 4 ? _t.Warning : _t.Text;
                _reserveText.text = "/ " + slot.Reserve;
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < weapons.Slots.Count; i++)
                {
                    var s = weapons.Slots[i];
                    string label = $"[{i + 1}] {ShortName(s.Data)}";
                    sb.Append(i == weapons.CurrentIndex ? $"<color=#{Hex(_t.Accent)}>{label}</color>" : label);
                    if (i < weapons.Slots.Count - 1) sb.Append("   ");
                }
                _slotsText.text = sb.ToString();
                _reloadText.text = weapons.IsReloading ? $"ПЕРЕЗАРЯДКА {weapons.ReloadProgress * 100:0}%"
                    : slot.Ammo == 0 && slot.Reserve == 0 ? "НЕТ ПАТРОНОВ" : slot.Ammo == 0 ? "[R] ПЕРЕЗАРЯДИТЬ" : "";
            }

            // Top bar.
            var state = tournament.State;
            int alive = tournament.AliveCount;
            _aliveText.text = state == MatchState.Lobby ? "ЛОББИ" : $"В ЖИВЫХ: {alive}";
            float time = tournament.MatchTime;
            _timerText.text = state == MatchState.Lobby ? "BoB" : $"{(int)(time / 60):00}:{(int)(time % 60):00}";

            UpdateZone(player);
            UpdateMinimap(player);
            UpdateTarget(cam);

            _centerTimer -= Time.deltaTime;
            float centerAlpha = Mathf.Clamp01(_centerTimer * 2f);
            _centerText.color = new Color(_t.Accent.r, _t.Accent.g, _t.Accent.b, centerAlpha);
            _subCenterText.color = new Color(_t.Text.r, _t.Text.g, _t.Text.b, centerAlpha);

            // Kill feed fade.
            for (int i = _feedItems.Count - 1; i >= 0; i--)
            {
                if (Time.time - _feedItems[i].time < 8f) continue;
                Destroy(_feedItems[i].go);
                _feedItems.RemoveAt(i);
            }

            // Stats panel (hold Tab).
            bool showStats = !InputGate.IsBlocked && gm.Input.Stats.IsPressed();
            _stats.alpha = Mathf.MoveTowards(_stats.alpha, showStats ? 1f : 0f, Time.deltaTime * 10f);
            if (showStats) _statsText.text = BuildStatsText();
        }

        private void UpdateZone(GGOPlayer player)
        {
            var zone = tournament.Zone;
            if (zone == null || !zone.IsRunning)
            {
                _zoneText.text = tournament.State == MatchState.Lobby ? "Терминал регистрации — в центре лобби" : "";
                _zoneVignette.color = Color.clear;
                return;
            }
            float outside = zone.DistanceToEdge(player.transform.position);
            if (outside > 0f && player.Health.IsAlive)
            {
                _zoneText.text = $"<color=#{Hex(_t.Negative)}>ВНЕ ЗОНЫ!  −{zone.CurrentDamage:0} HP/с  ·  до зоны {outside:0} м</color>";
                _zoneVignette.color = new Color(0.45f, 0.25f, 1f, 0.35f + 0.15f * Mathf.Sin(Time.time * 5f));
            }
            else
            {
                _zoneVignette.color = Color.clear;
                if (zone.IsShrinking) _zoneText.text = $"ЗОНА СУЖАЕТСЯ · этап {zone.PhaseNumber}/{zone.PhaseCount}";
                else if (zone.PhaseNumber <= zone.PhaseCount) _zoneText.text = $"Сужение зоны через {(int)zone.TimeToNextShrink / 60}:{(int)zone.TimeToNextShrink % 60:00}";
                else _zoneText.text = "Финальная зона";
            }
        }

        private void UpdateMinimap(GGOPlayer player)
        {
            float size = _minimap.rect.width * 0.5f;
            float scale = size / mapRadius;
            Vector2 ToMap(Vector3 world) => new Vector2(world.x - mapCenter.x, world.z - mapCenter.z) * scale;

            var zone = tournament.Zone;
            bool zoneActive = zone != null && zone.IsRunning;
            _zoneRing.gameObject.SetActive(zone != null);
            _nextRing.gameObject.SetActive(zoneActive && zone.PhaseNumber <= zone.PhaseCount);
            if (zone != null)
            {
                _zoneRing.PlaceAt(new Vector2(0.5f, 0.5f), ToMap(zone.Center), Vector2.one * zone.Radius * scale * 2f);
                if (zoneActive) _nextRing.PlaceAt(new Vector2(0.5f, 0.5f), ToMap(zone.NextCenter), Vector2.one * zone.NextRadius * scale * 2f);
            }
            Vector2 p = ToMap(player.transform.position);
            p = Vector2.ClampMagnitude(p, size - 6f);
            _playerDot.PlaceAt(new Vector2(0.5f, 0.5f), p, new Vector2(10, 10));
            _playerDot.localRotation = Quaternion.Euler(0, 0, -player.transform.eulerAngles.y);
        }

        private void UpdateTarget(Camera cam)
        {
            Participant target = null;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 150f, ~0, QueryTriggerInteraction.Ignore))
            {
                target = hit.collider.GetComponentInParent<Participant>();
                if (target != null && (target.IsPlayer || !target.IsAlive)) target = null;
            }
            _targetRoot.gameObject.SetActive(target != null);
            if (target != null)
            {
                _targetText.text = $"{target.DisplayName}  {Vector3.Distance(cam.transform.position, target.transform.position):0} м";
                _targetFill.rectTransform.anchorMax = new Vector2(target.Health.Normalized, 1f);
            }
        }

        private string BuildStatsText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<size=30><color=#{Hex(_t.Accent)}><b>BULLET OF BULLETS — УЧАСТНИКИ</b></color></size>\n");
            foreach (var p in tournament.Participants)
            {
                if (p == null) continue;
                string status = p.IsAlive ? $"<color=#{Hex(_t.Positive)}>ЖИВ</color>" : $"<color=#{Hex(_t.Negative)}>ВЫБЫЛ #{p.Placement}</color>";
                string name = p.IsPlayer ? $"<color=#{Hex(_t.Accent)}>{p.DisplayName}</color>" : p.DisplayName;
                sb.AppendLine($"{name}   ·   убийств: {p.Kills}   ·   {status}");
            }
            var progress = GameManager.Instance.Data.progress;
            var player = tournament.Player;
            sb.AppendLine($"\n<color=#{Hex(_t.Accent)}><b>ЭКИПИРОВКА</b></color>");
            foreach (var s in player.Weapons.Slots) sb.AppendLine($"• {s.Data.displayName} — {s.Ammo}/{s.Reserve}");
            sb.AppendLine($"• Броня: {(player.Armor != null ? player.Armor.displayName : "нет")} · Аптечки: {player.MedkitCount}");
            sb.AppendLine($"\n<color=#{Hex(_t.Accent)}><b>КАРЬЕРА</b></color>");
            sb.AppendLine($"Турниров: {progress.tournamentsPlayed} · побед: {progress.tournamentsWon} · убийств: {progress.totalKills} · кредиты: {GameManager.Instance.Currency.Credits}");
            return sb.ToString();
        }

        private static string ShortName(WeaponData data) => data.weaponType switch
        {
            WeaponType.Pistol => "ПИСТОЛЕТ",
            WeaponType.SniperRifle => "СНАЙПЕРКА",
            _ => "АВТОМАТ"
        };

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private static Sprite MakeScopeSprite()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Aspect-independent: the circle is drawn in a square texture stretched to the screen,
                // so use an ellipse that becomes a circle on 16:9 screens.
                float dx = (x + 0.5f - size / 2f) / (size / 2f) * (16f / 9f);
                float dy = (y + 0.5f - size / 2f) / (size / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.82f) * 30f);
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeVignetteSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
