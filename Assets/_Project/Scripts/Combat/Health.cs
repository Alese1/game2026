using System;
using UnityEngine;

namespace SwordArt.Combat
{
    /// <summary>Information about one damage event.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public GameObject Source;
        public Vector3 Point;
        public Vector3 Direction;
        public bool Headshot;
        public bool Environmental;
        /// <summary>Weapon-specific head multiplier (0 = use the hitbox default).</summary>
        public float HeadshotMultiplier;
    }

    /// <summary>Anything that can receive damage.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>
    /// Hit points with damage reduction, healing and death events. Used by the GGO player,
    /// bots and training dummies.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [Range(0f, 0.8f)] [SerializeField] private float damageReduction;
        [Tooltip("Regenerates to full after a delay (training dummies).")]
        [SerializeField] private bool autoRegenerate;
        [SerializeField] private float regenerateDelay = 3f;

        private float _current;
        private float _lastDamageTime;

        /// <summary>Raised after damage was applied (with final amount).</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>Raised once when health reaches zero.</summary>
        public event Action<DamageInfo> Died;

        /// <summary>Raised whenever current or max health changes.</summary>
        public event Action<Health> Changed;

        public float Current => _current;
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? _current / maxHealth : 0f;
        public bool IsAlive => _current > 0f;
        public float DamageReduction => damageReduction;

        /// <summary>When true, damage is ignored (e.g. lobby, countdown).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>Last damage event (killer info for kill feed).</summary>
        public DamageInfo LastDamage { get; private set; }

        private void Awake() => _current = maxHealth;

        /// <summary>Changes max health and optionally refills.</summary>
        public void SetMax(float max, bool refill)
        {
            maxHealth = Mathf.Max(1f, max);
            if (refill) _current = maxHealth;
            _current = Mathf.Min(_current, maxHealth);
            Changed?.Invoke(this);
        }

        /// <summary>Sets the fraction of damage absorbed.</summary>
        public void SetDamageReduction(float reduction) => damageReduction = Mathf.Clamp(reduction, 0f, 0.8f);

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || Invulnerable || info.Amount <= 0f) return;
            info.Amount *= 1f - damageReduction;
            _current = Mathf.Max(0f, _current - info.Amount);
            _lastDamageTime = Time.time;
            LastDamage = info;
            Damaged?.Invoke(info);
            Changed?.Invoke(this);
            if (_current <= 0f) Died?.Invoke(info);
        }

        /// <summary>Restores health. Returns the amount actually healed.</summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float before = _current;
            _current = Mathf.Min(maxHealth, _current + amount);
            Changed?.Invoke(this);
            return _current - before;
        }

        /// <summary>Brings the object back to full health.</summary>
        public void Revive()
        {
            _current = maxHealth;
            Changed?.Invoke(this);
        }

        private void Update()
        {
            if (!autoRegenerate || _current >= maxHealth) return;
            if (!IsAlive || Time.time - _lastDamageTime > regenerateDelay)
            {
                _current = maxHealth;
                Changed?.Invoke(this);
            }
        }
    }
}
