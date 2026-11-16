using SwordArt.Audio;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Inventory;
using SwordArt.Player;
using SwordArt.UI;
using SwordArt.Weapons;
using UnityEngine;

namespace SwordArt.Tournament
{
    /// <summary>
    /// The player's GGO avatar: builds the loadout from the shared inventory (weapons bought in
    /// the city, armour, consumables), handles medkits, match preparation and death.
    /// </summary>
    [RequireComponent(typeof(Participant), typeof(Health))]
    public sealed class GGOPlayer : MonoBehaviour
    {
        [SerializeField] private FirstPersonController motor;
        [SerializeField] private PlayerWeaponController weapons;
        [SerializeField] private float baseHealth = 100f;

        private Participant _participant;
        private Health _health;
        private int _extraMagazines;

        public Participant Participant => _participant != null ? _participant : _participant = GetComponent<Participant>();
        public Health Health => _health != null ? _health : _health = GetComponent<Health>();
        public PlayerWeaponController Weapons => weapons;
        public FirstPersonController Motor => motor;

        /// <summary>Equipped armour (null if none owned).</summary>
        public EquipmentData Armor { get; private set; }

        /// <summary>Configures references (used by the scene builder).</summary>
        public void Configure(FirstPersonController fps, PlayerWeaponController weaponController)
        {
            motor = fps;
            weapons = weaponController;
        }

        private void Awake()
        {
            _participant = GetComponent<Participant>();
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            Participant.Configure("Вы", true);
            ApplyEquipment();
            var owned = gm.Inventory.GetOwnedWeapons();
            weapons.Initialize(owned, gm.Inventory.SelectedWeaponId, 0);
        }

        private void ApplyEquipment()
        {
            var gm = GameManager.Instance;
            Armor = null;
            foreach (var (item, _) in gm.Inventory.GetAll())
                if (item is EquipmentData e && (Armor == null || e.damageReduction > Armor.damageReduction)) Armor = e;
            Health.SetMax(baseHealth + (Armor != null ? Armor.bonusMaxHealth : 0f), true);
            Health.SetDamageReduction(Armor != null ? Armor.damageReduction : 0f);
        }

        /// <summary>Lobby: free movement, invulnerable, unlimited practice.</summary>
        public void EnterLobby()
        {
            SetLocked(false);
            Health.Invulnerable = true;
        }

        /// <summary>Moves to the battlefield, refills ammo and consumes one ammo pack if owned.</summary>
        public void PrepareForMatch(Vector3 position, Quaternion rotation)
        {
            var gm = GameManager.Instance;
            _extraMagazines = 0;
            foreach (var (item, _) in gm.Inventory.GetAll())
            {
                if (item is ConsumableData c && c.kind == ConsumableKind.AmmoPack && gm.Inventory.Remove(c.id, 1))
                {
                    _extraMagazines = c.extraMagazines;
                    gm.UI.Notify($"Использован {c.displayName}: +{c.extraMagazines} магазина к каждому оружию", NotificationKind.Info);
                    break;
                }
            }
            weapons.RefillAll(_extraMagazines);
            ApplyEquipment();

            var context = FindAnyObjectByType<SceneContext>();
            if (context != null) context.Teleport(position, rotation);
            else transform.SetPositionAndRotation(position, rotation);
            Health.Invulnerable = true;
        }

        /// <summary>Locks/unlocks movement and shooting (countdown).</summary>
        public void SetLocked(bool locked)
        {
            motor.MovementLocked = locked;
            weapons.CanFire = !locked;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !Health.IsAlive || InputGate.IsBlocked) return;
            if (gm.Input.UseMedkit.WasPressedThisFrame()) UseMedkit();
        }

        /// <summary>Number of medkits in the inventory.</summary>
        public int MedkitCount
        {
            get
            {
                int n = 0;
                foreach (var (item, count) in GameManager.Instance.Inventory.GetAll())
                    if (item is ConsumableData c && c.kind == ConsumableKind.Medkit) n += count;
                return n;
            }
        }

        private void UseMedkit()
        {
            var gm = GameManager.Instance;
            if (Health.Current >= Health.Max)
            {
                gm.UI.Notify("Здоровье уже полное", NotificationKind.Warning);
                return;
            }
            foreach (var (item, _) in gm.Inventory.GetAll())
            {
                if (item is ConsumableData c && c.kind == ConsumableKind.Medkit && gm.Inventory.Remove(c.id, 1))
                {
                    float healed = Health.Heal(c.healAmount);
                    gm.Audio.Play2D(Sfx.Heal, 0.7f);
                    gm.UI.Notify($"Аптечка: +{healed:0} HP", NotificationKind.Success);
                    return;
                }
            }
            gm.UI.Notify("Нет аптечек — купите их в магазине", NotificationKind.Error);
        }

        private void OnDied(DamageInfo info)
        {
            motor.MovementLocked = true;
            motor.LookLocked = true;
            weapons.CanFire = false;
            weapons.SetVisible(false);
            CombatFX.Shatter(transform.position + Vector3.up, new Color(0.4f, 0.8f, 1f));
            StartCoroutine(DeathCamera());
        }

        private System.Collections.IEnumerator DeathCamera()
        {
            var cam = motor.Camera != null ? motor.Camera.transform : null;
            if (cam == null) yield break;
            Vector3 start = cam.localPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                cam.localPosition = Vector3.Lerp(start, start + new Vector3(0f, -1.2f, -0.3f), Mathf.SmoothStep(0f, 1f, t));
                cam.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-20f, 0f, 35f), t);
                yield return null;
            }
        }
    }
}
