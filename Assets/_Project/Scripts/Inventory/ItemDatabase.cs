using System.Collections.Generic;
using SwordArt.Economy;
using SwordArt.Weapons;
using UnityEngine;

namespace SwordArt.Inventory
{
    /// <summary>
    /// Registry of every item and shop product. Resolves save-file ids back to assets.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Items/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new List<ItemData>();
        public List<ShopItemData> shopItems = new List<ShopItemData>();

        private Dictionary<string, ItemData> _lookup;

        /// <summary>Returns the item with <paramref name="id"/> or null.</summary>
        public ItemData GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_lookup == null || _lookup.Count != items.Count) BuildLookup();
            return _lookup.TryGetValue(id, out var item) ? item : null;
        }

        /// <summary>Returns the weapon with <paramref name="id"/> or null.</summary>
        public WeaponData GetWeapon(string id) => GetItem(id) as WeaponData;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemData>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                if (_lookup.ContainsKey(item.id))
                {
                    Debug.LogWarning($"[ItemDatabase] Duplicate item id '{item.id}'.");
                    continue;
                }
                _lookup.Add(item.id, item);
            }
        }

        private void OnValidate() => _lookup = null;
    }
}
