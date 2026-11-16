using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.AI
{
    /// <summary>
    /// Designer-placed position behind an obstacle. Bots query these to break line of sight.
    /// </summary>
    public sealed class CoverPoint : MonoBehaviour
    {
        private static readonly List<CoverPoint> All = new List<CoverPoint>();

        /// <summary>Bot currently using the point (null = free).</summary>
        public Object Occupant { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>
        /// Best free cover near <paramref name="from"/> that hides a crouched/standing body from
        /// <paramref name="threat"/>. Returns null if none qualifies.
        /// </summary>
        public static CoverPoint FindBest(Vector3 from, Vector3 threat, float maxDistance, int obstacleMask, Object requester)
        {
            CoverPoint best = null;
            float bestScore = float.MaxValue;
            foreach (var cp in All)
            {
                if (cp == null || (cp.Occupant != null && cp.Occupant != requester)) continue;
                Vector3 p = cp.transform.position;
                float d = Vector3.Distance(from, p);
                if (d > maxDistance) continue;
                float threatDistance = Vector3.Distance(p, threat);
                if (threatDistance < 6f) continue;

                Vector3 eye = p + Vector3.up * 1.2f;
                Vector3 toThreat = threat - eye;
                bool hidden = Physics.Raycast(eye, toThreat.normalized, toThreat.magnitude - 0.5f, obstacleMask, QueryTriggerInteraction.Ignore);
                if (!hidden) continue;

                // Prefer close covers that do not require running toward the threat.
                float towardThreat = Vector3.Dot((p - from).normalized, (threat - from).normalized);
                float score = d + Mathf.Max(0f, towardThreat) * 8f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cp;
                }
            }
            return best;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f));
        }
    }
}
