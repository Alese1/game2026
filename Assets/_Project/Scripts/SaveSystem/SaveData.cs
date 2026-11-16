using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.SaveSystem
{
    /// <summary>
    /// Complete serialisable game state. Services operate on a single shared instance,
    /// so there is exactly one source of truth for credits and inventory.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Current save format version.</summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int credits;
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        [Tooltip("Weapon id selected in GGO.")] public string selectedWeapon;

        [Header("Real world")]
        public string lastRealScene;
        public bool hasRealWorldPosition;
        public Vector3 realWorldPosition;
        public float realWorldYaw;

        public ProgressData progress = new ProgressData();
        public string lastSaveTime;
    }

    /// <summary>One inventory stack.</summary>
    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count;
    }

    /// <summary>Long-term progression statistics.</summary>
    [Serializable]
    public class ProgressData
    {
        public int jobsCompleted;
        public int tournamentsPlayed;
        public int tournamentsWon;
        public int totalKills;
        public int bestPlacement;
        public int totalCreditsEarned;
    }
}
