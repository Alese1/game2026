using System;
using System.Collections.Generic;
using SwordArt.Combat;
using UnityEngine;

namespace SwordArt.Tournament
{
    /// <summary>One shrink stage of the safe zone.</summary>
    [Serializable]
    public class ZonePhase
    {
        [Tooltip("Seconds of waiting before shrinking starts.")] public float waitTime = 40f;
        [Tooltip("Seconds the shrink takes.")] public float shrinkTime = 25f;
        [Tooltip("Radius at the end of this phase.")] public float targetRadius = 60f;
        [Tooltip("Damage per second outside the zone during this phase.")] public float damagePerSecond = 3f;
    }

    /// <summary>
    /// Bullet-of-Bullets style shrinking play area: staged shrinking toward random sub-circles,
    /// periodic damage outside, and a translucent energy dome visual.
    /// </summary>
    public sealed class SafeZone : MonoBehaviour
    {
        [SerializeField] private float initialRadius = 120f;
        [SerializeField] private float damageTickInterval = 1f;
        [SerializeField] private List<ZonePhase> phases = new List<ZonePhase>();
        [Tooltip("Sphere/dome mesh scaled to the zone (radius 0.5 mesh = unit scale diameter).")]
        [SerializeField] private Transform domeVisual;
        [SerializeField] private Transform nextZoneMarker;

        private Vector3 _startCenter;
        private float _startRadius;
        private Vector3 _targetCenter;
        private float _targetRadius;
        private int _phaseIndex = -1;
        private float _phaseTimer;
        private bool _shrinking;
        private bool _running;
        private float _tickTimer;
        private readonly List<Participant> _participants = new List<Participant>();

        /// <summary>Current centre (on the ground plane).</summary>
        public Vector3 Center { get; private set; }

        /// <summary>Current radius in metres.</summary>
        public float Radius { get; private set; }

        public Vector3 NextCenter => _targetCenter;
        public float NextRadius => _targetRadius;
        public bool IsShrinking => _shrinking;
        public bool IsRunning => _running;
        public int PhaseNumber => _phaseIndex + 1;
        public int PhaseCount => phases.Count;

        /// <summary>Seconds until the next shrink starts (0 while shrinking).</summary>
        public float TimeToNextShrink => _running && !_shrinking && _phaseIndex < phases.Count ? Mathf.Max(0f, CurrentPhase.waitTime - _phaseTimer) : 0f;

        /// <summary>Damage per second outside the zone in the current phase.</summary>
        public float CurrentDamage => CurrentPhase != null ? CurrentPhase.damagePerSecond : 1f;

        private ZonePhase CurrentPhase => _phaseIndex >= 0 && _phaseIndex < phases.Count ? phases[_phaseIndex] : (phases.Count > 0 ? phases[phases.Count - 1] : null);

        private void Awake()
        {
            Center = new Vector3(transform.position.x, 0f, transform.position.z);
            Radius = initialRadius;
            _targetCenter = Center;
            _targetRadius = Radius;
            UpdateVisual();
        }

        /// <summary>Replaces the phase list (used by builders).</summary>
        public void Configure(float radius, List<ZonePhase> stages, Transform dome, Transform marker)
        {
            initialRadius = radius;
            phases = stages;
            domeVisual = dome;
            nextZoneMarker = marker;
        }

        /// <summary>Starts the zone cycle for the given participants.</summary>
        public void Begin(IEnumerable<Participant> participants)
        {
            _participants.Clear();
            _participants.AddRange(participants);
            Center = new Vector3(transform.position.x, 0f, transform.position.z);
            Radius = initialRadius;
            _running = true;
            StartPhase(0);
        }

        /// <summary>Stops shrinking and damage.</summary>
        public void Stop() => _running = false;

        /// <summary>True when <paramref name="position"/> is inside the zone (horizontal distance).</summary>
        public bool IsInside(Vector3 position) => DistanceToEdge(position) <= 0f;

        /// <summary>Positive distance outside the edge, negative inside.</summary>
        public float DistanceToEdge(Vector3 position)
        {
            Vector3 d = position - Center;
            d.y = 0f;
            return d.magnitude - Radius;
        }

        /// <summary>Random point inside the (next) safe circle, shrunk by <paramref name="margin"/>.</summary>
        public Vector3 RandomPointInside(float margin, bool useNextZone)
        {
            Vector3 c = useNextZone ? _targetCenter : Center;
            float r = Mathf.Max(1f, (useNextZone ? _targetRadius : Radius) - margin);
            Vector2 p = UnityEngine.Random.insideUnitCircle * r;
            return new Vector3(c.x + p.x, 0f, c.z + p.y);
        }

        private void StartPhase(int index)
        {
            _phaseIndex = index;
            _phaseTimer = 0f;
            _shrinking = false;
            _startCenter = Center;
            _startRadius = Radius;
            if (index < phases.Count)
            {
                _targetRadius = phases[index].targetRadius;
                float maxOffset = Mathf.Max(0f, Radius - _targetRadius);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * maxOffset * 0.8f;
                _targetCenter = Center + new Vector3(offset.x, 0f, offset.y);
            }
        }

        private void Update()
        {
            if (!_running) return;
            var phase = CurrentPhase;
            if (_phaseIndex < phases.Count && phase != null)
            {
                _phaseTimer += Time.deltaTime;
                if (!_shrinking && _phaseTimer >= phase.waitTime)
                {
                    _shrinking = true;
                    _phaseTimer = 0f;
                }
                if (_shrinking)
                {
                    float t = Mathf.Clamp01(_phaseTimer / Mathf.Max(0.01f, phase.shrinkTime));
                    Center = Vector3.Lerp(_startCenter, _targetCenter, t);
                    Radius = Mathf.Lerp(_startRadius, _targetRadius, t);
                    if (t >= 1f) StartPhase(_phaseIndex + 1);
                }
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= damageTickInterval)
            {
                _tickTimer = 0f;
                ApplyZoneDamage();
            }
            UpdateVisual();
        }

        private void ApplyZoneDamage()
        {
            float damage = CurrentDamage * damageTickInterval;
            foreach (var p in _participants)
            {
                if (p == null || !p.IsAlive || IsInside(p.transform.position)) continue;
                p.Health.TakeDamage(new DamageInfo { Amount = damage, Environmental = true, Point = p.ChestPosition });
            }
        }

        private void UpdateVisual()
        {
            if (domeVisual != null)
            {
                domeVisual.position = Center;
                domeVisual.localScale = Vector3.one * Radius * 2f;
            }
            if (nextZoneMarker != null)
            {
                bool show = _running && _phaseIndex < phases.Count;
                nextZoneMarker.gameObject.SetActive(show);
                if (show)
                {
                    nextZoneMarker.position = _targetCenter + Vector3.up * 0.15f;
                    nextZoneMarker.localScale = new Vector3(_targetRadius * 2f, 1f, _targetRadius * 2f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Vector3 c = Application.isPlaying ? Center : transform.position;
            float r = Application.isPlaying ? Radius : initialRadius;
            const int seg = 64;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
                Gizmos.DrawLine(c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r);
            }
        }
    }
}
