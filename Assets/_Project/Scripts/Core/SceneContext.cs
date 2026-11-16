using SwordArt.World;
using UnityEngine;

namespace SwordArt.Core
{
    /// <summary>
    /// Per-scene entry point: tells the <see cref="GameManager"/> which world is active and
    /// places the player at the requested spawn point (or the saved real-world position).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class SceneContext : MonoBehaviour
    {
        [SerializeField] private WorldType worldType = WorldType.RealWorld;
        [Tooltip("Player root that is moved to the spawn point.")]
        [SerializeField] private Transform player;
        [Tooltip("Spawn used when no other spawn matches.")]
        [SerializeField] private SpawnPoint defaultSpawn;

        public WorldType WorldType => worldType;
        public Transform Player => player;

        private void Awake()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.EnterScene(this);
            PlacePlayer(gm);
        }

        private void PlacePlayer(GameManager gm)
        {
            if (player == null) return;
            string spawnId = gm.PendingSpawnId;
            gm.PendingSpawnId = SpawnIds.Saved;

            var data = gm.Data;
            string sceneName = gameObject.scene.name;
            if (spawnId == SpawnIds.Saved && worldType == WorldType.RealWorld && data != null &&
                data.hasRealWorldPosition && data.lastRealScene == sceneName)
            {
                Teleport(data.realWorldPosition, Quaternion.Euler(0f, data.realWorldYaw, 0f));
                return;
            }

            var spawn = SpawnPoint.Find(spawnId) ?? defaultSpawn;
            if (spawn != null) Teleport(spawn.transform.position, spawn.transform.rotation);
        }

        /// <summary>Moves the player safely (CharacterController must be disabled while teleporting).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            var cc = player.GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            player.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            if (cc != null) cc.enabled = wasEnabled;
            foreach (var receiver in player.GetComponentsInChildren<ITeleportListener>())
                receiver.OnTeleported();
        }
    }

    /// <summary>Components that must reset internal state (e.g. camera yaw) after a teleport.</summary>
    public interface ITeleportListener
    {
        void OnTeleported();
    }
}
