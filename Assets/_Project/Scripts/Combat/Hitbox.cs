using UnityEngine;

namespace SwordArt.Combat
{
    /// <summary>
    /// Collider part of a character (e.g. head) that forwards damage to its <see cref="Health"/>
    /// with a multiplier.
    /// </summary>
    public sealed class Hitbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health owner;
        [SerializeField] private float multiplier = 2f;

        public Health Owner => owner;
        public float Multiplier => multiplier;
        public bool IsAlive => owner != null && owner.IsAlive;

        /// <summary>Configures the hitbox (used by builders).</summary>
        public void Configure(Health health, float damageMultiplier)
        {
            owner = health;
            multiplier = damageMultiplier;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (owner == null) return;
            float m = multiplier > 1f && info.HeadshotMultiplier > 0f ? info.HeadshotMultiplier : multiplier;
            info.Amount *= m;
            info.Headshot = multiplier > 1f;
            owner.TakeDamage(info);
        }
    }
}
