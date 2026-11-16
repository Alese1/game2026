using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>Named location where the player appears after a scene transition.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        private static readonly List<SpawnPoint> All = new List<SpawnPoint>();

        [SerializeField] private string id = "Default";

        public string Id => id;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Finds an enabled spawn point by id (searches the scene if not yet registered).</summary>
        public static SpawnPoint Find(string spawnId)
        {
            foreach (var s in All)
                if (s != null && s.id == spawnId) return s;
            foreach (var s in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
                if (s.id == spawnId) return s;
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.4f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.9f, transform.forward);
        }
    }
}
