using SwordArt.Economy;
using SwordArt.Inventory;
using UnityEngine;

namespace SwordArt.Core
{
    /// <summary>
    /// Root configuration asset loaded from Resources by <see cref="GameManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        /// <summary>Resources path of the config asset.</summary>
        public const string ResourcePath = "GameConfig";

        public ItemDatabase itemDatabase;
        public EconomyConfig economy;

        [Header("Shared FX materials")]
        [Tooltip("Additive particle material (muzzle flashes, sparks, shards).")]
        public Material particleAdditive;
        [Tooltip("Alpha-blended particle material (dust, smoke).")]
        public Material particleAlpha;
        [Tooltip("Unlit additive material for bullet tracers and bullet lines.")]
        public Material tracerMaterial;
        [Tooltip("Untextured additive material: hard-edged 'polygon' shards for avatar hits and deaths.")]
        public Material shardMaterial;
    }
}
