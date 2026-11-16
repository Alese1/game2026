using UnityEngine;

namespace SwordArt.Inventory
{
    /// <summary>Category of an inventory item.</summary>
    public enum ItemType
    {
        Weapon,
        Equipment,
        Consumable,
        Cosmetic
    }

    /// <summary>
    /// Base definition for anything that can be stored in the player's inventory.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Items/Generic Item", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Unique, stable identifier used in save files.")]
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        [Tooltip("Colour used for generated icons and UI accents.")]
        public Color accentColor = Color.white;
        public ItemType itemType = ItemType.Consumable;
        [Tooltip("Maximum amount that can be stacked. 1 = unique item.")]
        [Min(1)] public int maxStack = 1;

        /// <summary>True when only a single copy of this item can be owned.</summary>
        public bool IsUnique => maxStack <= 1;
    }
}
