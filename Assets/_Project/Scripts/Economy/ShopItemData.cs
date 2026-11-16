using SwordArt.Inventory;
using UnityEngine;

namespace SwordArt.Economy
{
    /// <summary>
    /// A product sold in the city shop. Points at the <see cref="ItemData"/> that is granted on purchase.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Economy/Shop Item", fileName = "Shop_")]
    public class ShopItemData : ScriptableObject
    {
        [Tooltip("Unique product identifier.")] public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        [Min(0)] public int price = 100;
        public Sprite icon;
        public ItemType itemType;
        [Tooltip("Item granted to the inventory.")] public ItemData item;
        [Min(1)] public int quantity = 1;

        /// <summary>Icon to show: the product icon or the item's icon.</summary>
        public Sprite DisplayIcon => icon != null ? icon : (item != null ? item.icon : null);
    }
}
