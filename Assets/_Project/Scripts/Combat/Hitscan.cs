using System;
using UnityEngine;

namespace SwordArt.Combat
{
    /// <summary>Outcome of a hitscan shot.</summary>
    public struct ShotResult
    {
        public bool Hit;
        public Vector3 Point;
        public Vector3 Normal;
        public SurfaceType Surface;
        public Health Victim;
        public bool Headshot;
        public bool Killed;
    }

    /// <summary>
    /// Shared raycast ballistics for the player and bots: ignores the shooter's own colliders,
    /// applies damage through <see cref="IDamageable"/>, spawns impact FX and notifies listeners
    /// (AI hearing) about gunshots.
    /// </summary>
    public static class Hitscan
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>Raised for every shot: (origin, shooter root).</summary>
        public static event Action<Vector3, GameObject> ShotFired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ShotFired = null;

        /// <summary>Fires one ray and applies damage to the first valid hit.</summary>
        public static ShotResult Fire(Vector3 origin, Vector3 direction, float range, float damage, GameObject shooter, int mask = ~0, float headshotMultiplier = 0f)
        {
            ShotFired?.Invoke(origin, shooter);
            var result = new ShotResult { Point = origin + direction * range };

            int count = Physics.RaycastNonAlloc(origin, direction, Hits, range, mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = Hits[i];
                if (shooter != null && h.collider.transform.IsChildOf(shooter.transform)) continue;
                if (h.distance < bestDistance)
                {
                    bestDistance = h.distance;
                    best = i;
                }
            }
            if (best < 0) return result;

            var hit = Hits[best];
            result.Hit = true;
            result.Point = hit.point;
            result.Normal = hit.normal;
            result.Surface = SurfaceTag.Resolve(hit.collider);

            var damageable = hit.collider.TryGetComponent<IDamageable>(out var direct) ? direct : hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                var health = damageable as Health ?? (damageable as Hitbox)?.Owner;
                bool wasAlive = health != null && health.IsAlive;
                damageable.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Source = shooter,
                    Point = hit.point,
                    Direction = direction,
                    HeadshotMultiplier = headshotMultiplier
                });
                result.Victim = health;
                result.Headshot = damageable is Hitbox hb && hb.Multiplier > 1f;
                result.Killed = wasAlive && health != null && !health.IsAlive;
            }

            CombatFX.Impact(hit.point, hit.normal, result.Surface);
            return result;
        }

        /// <summary>Random direction inside a cone of <paramref name="spreadDegrees"/> around <paramref name="forward"/>.</summary>
        public static Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return forward;
            Vector2 r = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            var rot = Quaternion.LookRotation(forward);
            return (forward + rot * new Vector3(r.x, r.y, 0f)).normalized;
        }
    }
}
