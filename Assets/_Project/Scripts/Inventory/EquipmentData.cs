using UnityEngine;

namespace SwordArt.Inventory
{
    /// <summary>Passive gear that modifies the avatar in GGO (e.g. body armour).</summary>
    [CreateAssetMenu(menuName = "SwordArt/Items/Equipment", fileName = "Equip_")]
    public class EquipmentData : ItemData
    {
        [Tooltip("Additional maximum health in GGO.")]
        public float bonusMaxHealth;
        [Tooltip("Fraction of incoming damage that is absorbed (0..0.8).")]
        [Range(0f, 0.8f)] public float damageReduction;
    }
}
