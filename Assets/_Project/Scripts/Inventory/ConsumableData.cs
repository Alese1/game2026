using UnityEngine;

namespace SwordArt.Inventory
{
    /// <summary>Kinds of consumables.</summary>
    public enum ConsumableKind
    {
        Medkit,
        AmmoPack
    }

    /// <summary>Single-use items such as medkits.</summary>
    [CreateAssetMenu(menuName = "SwordArt/Items/Consumable", fileName = "Consumable_")]
    public class ConsumableData : ItemData
    {
        public ConsumableKind kind;
        [Tooltip("Health restored by a medkit.")]
        public float healAmount = 50f;
        [Tooltip("Extra reserve magazines granted by an ammo pack.")]
        public int extraMagazines = 2;
    }
}
