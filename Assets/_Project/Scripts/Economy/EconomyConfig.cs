using System.Collections.Generic;
using SwordArt.Inventory;
using UnityEngine;

namespace SwordArt.Economy
{
    /// <summary>
    /// Economy balance: starting resources, job pay and tournament rewards.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Economy/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("New game")]
        public int startingCredits = 250;
        [Tooltip("Items every new game starts with (e.g. starter pistol).")]
        public List<ItemData> starterItems = new List<ItemData>();

        [Header("Office job")]
        [Tooltip("Credits for each completed data entry code.")] public int jobRewardPerTask = 15;
        [Tooltip("Bonus when every task of a shift is completed.")] public int jobCompletionBonus = 30;
        [Tooltip("Bonus when the shift is completed without mistakes.")] public int jobPerfectBonus = 20;
        [Min(1)] public int jobTasksPerShift = 6;
        public float jobShiftDuration = 35f;

        [Header("Tournament")]
        public int participationReward = 50;
        public int rewardPerKill = 35;
        public int winReward = 500;
        [Tooltip("Bonus for finishing in the top 3 (without winning).")] public int podiumReward = 100;
    }
}
