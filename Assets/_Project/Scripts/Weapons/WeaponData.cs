using SwordArt.Inventory;
using UnityEngine;

namespace SwordArt.Weapons
{
    /// <summary>Weapon archetype.</summary>
    public enum WeaponType
    {
        Pistol,
        AssaultRifle,
        SniperRifle
    }

    /// <summary>
    /// Ballistic and handling parameters of a GGO firearm. Shared by the player and bots.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/Items/Weapon", fileName = "Weapon_")]
    public class WeaponData : ItemData
    {
        [Header("Combat")]
        public WeaponType weaponType;
        [Tooltip("Damage per hit.")] public float damage = 20f;
        [Tooltip("Shots per second.")] public float fireRate = 8f;
        [Tooltip("Maximum effective range in metres.")] public float range = 120f;
        [Tooltip("Hip-fire spread cone half-angle in degrees.")] public float spread = 2f;
        [Tooltip("Spread while aiming down sights, in degrees.")] public float aimSpread = 0.5f;
        public bool automatic = true;
        [Min(1)] public int magazineSize = 30;
        [Tooltip("Magazines carried in reserve at match start.")] public int reserveMagazines = 4;
        public float reloadTime = 2f;
        [Tooltip("Head hit damage multiplier.")] public float headshotMultiplier = 2f;

        [Header("Recoil")]
        [Tooltip("Vertical camera kick in degrees per shot.")] public float recoil = 1f;
        [Tooltip("Random horizontal kick in degrees per shot.")] public float recoilHorizontal = 0.3f;

        [Header("Handling")]
        public float aimFov = 50f;
        [Tooltip("Uses a full-screen scope overlay when aiming.")] public bool hasScope;
        [Tooltip("Movement speed multiplier while holding this weapon.")] public float mobility = 1f;

        [Header("Presentation")]
        [Tooltip("Model used in first person and in bots' hands.")] public GameObject modelPrefab;
        public Vector3 viewOffset = new Vector3(0.22f, -0.2f, 0.45f);
        public Vector3 aimOffset = new Vector3(0f, -0.11f, 0.32f);
        [Tooltip("Muzzle position relative to the model root.")] public Vector3 muzzleLocalPosition = new Vector3(0f, 0.05f, 0.6f);
        public Color tracerColor = new Color(1f, 0.8f, 0.4f);
        [Tooltip("Pitch of the procedural gunshot sound.")] public float soundPitch = 1f;

        /// <summary>Seconds between consecutive shots.</summary>
        public float FireInterval => 1f / Mathf.Max(0.01f, fireRate);
    }
}
