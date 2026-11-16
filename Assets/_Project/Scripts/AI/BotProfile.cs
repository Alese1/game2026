using SwordArt.Weapons;
using UnityEngine;

namespace SwordArt.AI
{
    /// <summary>
    /// Behaviour and loadout tuning for a class of tournament bots (rusher, rifleman, sniper...).
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/AI/Bot Profile", fileName = "Bot_")]
    public class BotProfile : ScriptableObject
    {
        public string archetype = "Rifleman";
        public WeaponData weapon;
        public float maxHealth = 100f;

        [Header("Perception")]
        public float sightRange = 70f;
        [Range(30f, 360f)] public float fieldOfView = 120f;
        public float hearingRange = 45f;
        [Tooltip("Delay between spotting a target and opening fire.")] public float reactionTime = 0.6f;

        [Header("Combat")]
        [Tooltip("Multiplies the weapon's spread (higher = less accurate).")] public float spreadMultiplier = 2.2f;
        [Tooltip("Multiplies weapon damage to keep bots fair.")] public float damageMultiplier = 0.55f;
        [Tooltip("Seconds the red bullet line is shown before a burst.")] public float aimTime = 0.6f;
        public Vector2Int burstSize = new Vector2Int(3, 6);
        public Vector2 burstPause = new Vector2(0.5f, 1.2f);
        public float preferredMinDistance = 10f;
        public float preferredMaxDistance = 30f;
        [Range(0f, 1f)] [Tooltip("Health fraction below which the bot seeks cover.")] public float coverHealthThreshold = 0.5f;

        [Header("Movement")]
        public float walkSpeed = 3.2f;
        public float runSpeed = 5.6f;

        [Header("Look")]
        public Color outfitColor = new Color(0.35f, 0.38f, 0.3f);
        public Color accentColor = new Color(0.9f, 0.4f, 0.2f);
    }
}
