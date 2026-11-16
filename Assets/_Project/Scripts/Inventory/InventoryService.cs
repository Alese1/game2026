using System;
using System.Collections.Generic;
using SwordArt.SaveSystem;
using SwordArt.Weapons;
using UnityEngine;

namespace SwordArt.Inventory
{
    /// <summary>Storage of owned items.</summary>
    public interface IInventoryService
    {
        /// <summary>Raised after any change to the inventory.</summary>
        event Action Changed;

        int GetCount(string itemId);
        bool Has(string itemId);
        bool CanAdd(ItemData item, int amount);
        bool Add(ItemData item, int amount);
        bool Remove(string itemId, int amount);
        IEnumerable<(ItemData item, int count)> GetAll();
    }

    /// <summary>
    /// Inventory stored in the bound <see cref="SaveData"/>. Item ids are validated against the
    /// <see cref="ItemDatabase"/> so stale save entries are dropped safely.
    /// </summary>
    public sealed class InventoryService : IInventoryService
    {
        private readonly ItemDatabase _database;
        private SaveData _data;

        public event Action Changed;

        public InventoryService(ItemDatabase database) => _database = database;

        /// <summary>Points the service at a save and removes unknown items.</summary>
        public void Bind(SaveData data)
        {
            _data = data;
            if (_data != null && _database != null)
            {
                int removed = _data.inventory.RemoveAll(e => _database.GetItem(e.itemId) == null);
                if (removed > 0) Debug.LogWarning($"[Inventory] Removed {removed} unknown item(s) from save.");
            }
            Changed?.Invoke();
        }

        public int GetCount(string itemId)
        {
            var entry = Find(itemId);
            return entry?.count ?? 0;
        }

        public bool Has(string itemId) => GetCount(itemId) > 0;

        public bool CanAdd(ItemData item, int amount)
        {
            if (_data == null || item == null || amount <= 0) return false;
            return GetCount(item.id) + amount <= item.maxStack;
        }

        public bool Add(ItemData item, int amount)
        {
            if (!CanAdd(item, amount)) return false;
            var entry = Find(item.id);
            if (entry == null)
            {
                entry = new InventoryEntry { itemId = item.id, count = 0 };
                _data.inventory.Add(entry);
            }
            entry.count += amount;
            if (item is WeaponData && string.IsNullOrEmpty(_data.selectedWeapon)) _data.selectedWeapon = item.id;
            Changed?.Invoke();
            return true;
        }

        public bool Remove(string itemId, int amount)
        {
            var entry = Find(itemId);
            if (entry == null || amount <= 0 || entry.count < amount) return false;
            entry.count -= amount;
            if (entry.count == 0) _data.inventory.Remove(entry);
            Changed?.Invoke();
            return true;
        }

        public IEnumerable<(ItemData item, int count)> GetAll()
        {
            if (_data == null || _database == null) yield break;
            foreach (var e in _data.inventory)
            {
                var item = _database.GetItem(e.itemId);
                if (item != null) yield return (item, e.count);
            }
        }

        /// <summary>All owned weapons, ordered pistol → rifle → sniper.</summary>
        public List<WeaponData> GetOwnedWeapons()
        {
            var list = new List<WeaponData>();
            foreach (var (item, _) in GetAll())
                if (item is WeaponData w) list.Add(w);
            list.Sort((a, b) => a.weaponType.CompareTo(b.weaponType));
            return list;
        }

        /// <summary>Weapon id selected for GGO.</summary>
        public string SelectedWeaponId
        {
            get => _data?.selectedWeapon;
            set { if (_data != null) _data.selectedWeapon = value; }
        }

        private InventoryEntry Find(string itemId)
        {
            if (_data == null || string.IsNullOrEmpty(itemId)) return null;
            foreach (var e in _data.inventory)
                if (e.itemId == itemId) return e;
            return null;
        }
    }
}
