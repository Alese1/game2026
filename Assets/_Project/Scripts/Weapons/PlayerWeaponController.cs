using System;
using System.Collections.Generic;
using SwordArt.Audio;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Player;
using UnityEngine;

namespace SwordArt.Weapons
{
    /// <summary>
    /// First-person weapon handling in GGO: owned weapon slots, hitscan firing with dynamic
    /// spread ("bullet circle"), recoil, reloading, aiming down sights (incl. sniper scope),
    /// switching, weapon sway/bob and hit feedback.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        /// <summary>Runtime state of one carried weapon.</summary>
        public sealed class Slot
        {
            public WeaponData Data;
            public GameObject View;
            public Transform Muzzle;
            public int Ammo;
            public int Reserve;
        }

        [SerializeField] private FirstPersonController motor;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private float baseFov = 72f;
        [SerializeField] private string weaponLayerName = "FPWeapon";
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly List<Slot> _slots = new List<Slot>();
        private int _current = -1;
        private float _nextFire;
        private bool _reloading;
        private float _reloadEnd;
        private float _aim;
        private float _bloom;
        private float _switchBlend = 1f;
        private int _pendingSwitch = -1;
        private Vector3 _kick;
        private Vector3 _sway;
        private bool _hidden;
        private float _bobPhase;

        /// <summary>Raised when ammo, reload state or the active weapon changes.</summary>
        public event Action Changed;

        /// <summary>Raised when a shot hits a damageable: (killed, headshot).</summary>
        public event Action<bool, bool> HitConfirmed;

        /// <summary>When false the weapon cannot be fired (lobby countdown, death).</summary>
        public bool CanFire { get; set; } = true;

        public IReadOnlyList<Slot> Slots => _slots;
        public Slot Current => _current >= 0 && _current < _slots.Count ? _slots[_current] : null;
        public int CurrentIndex => _current;
        public bool IsReloading => _reloading;
        public float ReloadProgress => _reloading && Current != null ? 1f - Mathf.Clamp01((_reloadEnd - Time.time) / Current.Data.reloadTime) : 0f;
        public float AimAmount => _aim;
        public bool IsScoped => Current != null && Current.Data.hasScope && _aim > 0.85f;

        /// <summary>Current spread half-angle in degrees (drives the HUD bullet circle).</summary>
        public float CurrentSpread { get; private set; }

        /// <summary>Equips the given weapons. Extra magazines come from ammo packs.</summary>
        public void Initialize(IList<WeaponData> weapons, string selectedId, int extraMagazines)
        {
            foreach (var s in _slots)
                if (s.View != null) Destroy(s.View);
            _slots.Clear();

            int layer = LayerMask.NameToLayer(weaponLayerName);
            foreach (var data in weapons)
            {
                if (data == null) continue;
                var view = CreateView(data, layer);
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(view.transform, false);
                muzzle.localPosition = data.muzzleLocalPosition;
                view.SetActive(false);
                _slots.Add(new Slot
                {
                    Data = data,
                    View = view,
                    Muzzle = muzzle,
                    Ammo = data.magazineSize,
                    Reserve = data.magazineSize * Mathf.Max(0, data.reserveMagazines + extraMagazines)
                });
            }

            int index = _slots.FindIndex(s => s.Data.id == selectedId);
            _current = -1;
            Equip(index >= 0 ? index : 0, true);
        }

        /// <summary>Shows or hides the first-person weapon (hidden after death).</summary>
        public void SetVisible(bool visible)
        {
            _hidden = !visible;
            if (Current != null) Current.View.SetActive(visible);
        }

        /// <summary>Refills all magazines and reserves (new round).</summary>
        public void RefillAll(int extraMagazines)
        {
            foreach (var s in _slots)
            {
                s.Ammo = s.Data.magazineSize;
                s.Reserve = s.Data.magazineSize * Mathf.Max(0, s.Data.reserveMagazines + extraMagazines);
            }
            _reloading = false;
            Changed?.Invoke();
        }

        private GameObject CreateView(WeaponData data, int layer)
        {
            GameObject view;
            if (data.modelPrefab != null)
            {
                view = Instantiate(data.modelPrefab, weaponHolder);
                view.name = "View_" + data.id;
            }
            else
            {
                view = WeaponPlaceholder.Create(data, weaponHolder);
            }
            view.transform.localPosition = data.viewOffset;
            view.transform.localRotation = Quaternion.identity;
            foreach (var t in view.GetComponentsInChildren<Transform>(true))
                if (layer >= 0) t.gameObject.layer = layer;
            foreach (var r in view.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var c in view.GetComponentsInChildren<Collider>(true))
                Destroy(c);
            return view;
        }

        private void Equip(int index, bool instant)
        {
            if (index < 0 || index >= _slots.Count || index == _current) return;
            if (!instant && _current >= 0)
            {
                _pendingSwitch = index;
                return;
            }
            if (Current != null) Current.View.SetActive(false);
            _current = index;
            _reloading = false;
            Current.View.SetActive(true);
            _switchBlend = instant ? 1f : 0f;
            _nextFire = Time.time + 0.25f;
            var gm = GameManager.Instance;
            if (gm != null) gm.Inventory.SelectedWeaponId = Current.Data.id;
            Changed?.Invoke();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            var slot = Current;
            if (gm == null || slot == null || _hidden) return;
            bool input = !InputGate.IsBlocked && CanFire;
            var actions = gm.Input;

            HandleSwitching(input, actions);

            bool aiming = input && actions.Aim.IsPressed() && !_reloading && _switchBlend > 0.9f;
            _aim = Mathf.MoveTowards(_aim, aiming ? 1f : 0f, Time.deltaTime * (slot.Data.hasScope ? 4.5f : 7f));
            float targetFov = Mathf.Lerp(baseFov, slot.Data.aimFov, _aim);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * 18f);

            if (motor != null)
            {
                motor.SpeedMultiplier = slot.Data.mobility * Mathf.Lerp(1f, 0.55f, _aim);
                motor.SprintBlocked = _aim > 0.1f || (input && actions.Fire.IsPressed());
            }

            if (_reloading && Time.time >= _reloadEnd) FinishReload();

            if (input && actions.Reload.WasPressedThisFrame()) StartReload();

            bool trigger = input && (slot.Data.automatic ? actions.Fire.IsPressed() : actions.Fire.WasPressedThisFrame());
            if (trigger && Time.time >= _nextFire && !_reloading && _switchBlend > 0.9f)
            {
                if (slot.Ammo > 0) Fire(slot);
                else
                {
                    _nextFire = Time.time + 0.3f;
                    gm.Audio.Play2D(Sfx.DryFire, 0.6f);
                    StartReload();
                }
            }

            UpdateSpread(slot);
            UpdateViewModel(slot, actions);
        }

        private void HandleSwitching(bool input, GameInput actions)
        {
            if (_pendingSwitch >= 0)
            {
                _switchBlend = Mathf.MoveTowards(_switchBlend, 0f, Time.deltaTime * 6f);
                if (_switchBlend <= 0f)
                {
                    int next = _pendingSwitch;
                    _pendingSwitch = -1;
                    if (Current != null) Current.View.SetActive(false);
                    _current = -1;
                    Equip(next, true);
                    _switchBlend = 0f;
                }
                return;
            }
            _switchBlend = Mathf.MoveTowards(_switchBlend, 1f, Time.deltaTime * 5f);
            if (!input || _slots.Count < 2) return;

            if (actions.Weapon1.WasPressedThisFrame()) Equip(0, false);
            else if (actions.Weapon2.WasPressedThisFrame()) Equip(1, false);
            else if (actions.Weapon3.WasPressedThisFrame()) Equip(2, false);
            else
            {
                float scroll = actions.ScrollWeapon.ReadValue<float>();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    int dir = scroll > 0 ? -1 : 1;
                    Equip((_current + dir + _slots.Count) % _slots.Count, false);
                }
            }
        }

        private void Fire(Slot slot)
        {
            var data = slot.Data;
            _nextFire = Time.time + data.FireInterval;
            slot.Ammo--;

            Vector3 origin = playerCamera.transform.position;
            Vector3 dir = Hitscan.ApplySpread(playerCamera.transform.forward, CurrentSpread);
            // Headshot multiplier is applied by the target's Hitbox.
            var result = Hitscan.Fire(origin, dir, data.range, data.damage, motor != null ? motor.gameObject : gameObject, hitMask, data.headshotMultiplier);

            Vector3 muzzle = slot.Muzzle.position;
            CombatFX.MuzzleFlash(muzzle, playerCamera.transform.forward, data.tracerColor);
            CombatFX.Tracer(muzzle, result.Point, data.tracerColor);

            var audio = GameManager.Instance.Audio;
            Sfx shot = data.weaponType switch
            {
                WeaponType.Pistol => Sfx.GunshotPistol,
                WeaponType.SniperRifle => Sfx.GunshotSniper,
                _ => Sfx.GunshotRifle
            };
            audio.Play2D(shot, 0.55f, data.soundPitch * UnityEngine.Random.Range(0.95f, 1.05f));

            float aimFactor = Mathf.Lerp(1f, 0.55f, _aim);
            motor?.AddRecoil(data.recoil * aimFactor, UnityEngine.Random.Range(-data.recoilHorizontal, data.recoilHorizontal) * aimFactor);
            _kick += new Vector3(0f, 0.01f, -0.06f) * (0.6f + data.recoil * 0.4f);
            _bloom = Mathf.Min(_bloom + data.recoil * 0.45f, data.spread * 2.5f + 2f);

            if (result.Victim != null)
            {
                if (result.Killed) audio.Play2D(Sfx.KillConfirm, 0.7f);
                else audio.Play2D(Sfx.HitMarker, 0.6f, result.Headshot ? 1.4f : 1f);
                HitConfirmed?.Invoke(result.Killed, result.Headshot);
            }

            if (slot.Ammo == 0 && slot.Reserve > 0) StartReload(0.15f);
            Changed?.Invoke();
        }

        private void UpdateSpread(Slot slot)
        {
            var data = slot.Data;
            _bloom = Mathf.MoveTowards(_bloom, 0f, Time.deltaTime * (3f + data.recoil * 2f));
            float baseSpread = Mathf.Lerp(data.spread, data.aimSpread, _aim);
            float movePenalty = 0f;
            if (motor != null)
            {
                movePenalty = Mathf.Clamp01(motor.HorizontalSpeed / motor.RunSpeed) * data.spread * Mathf.Lerp(1f, 0.4f, _aim);
                if (!motor.IsGrounded) movePenalty += 3f;
            }
            CurrentSpread = baseSpread + movePenalty + _bloom * Mathf.Lerp(1f, 0.6f, _aim);
        }

        /// <summary>Starts reloading if possible.</summary>
        public void StartReload(float extraDelay = 0f)
        {
            var slot = Current;
            if (slot == null || _reloading || slot.Ammo >= slot.Data.magazineSize || slot.Reserve <= 0) return;
            _reloading = true;
            _reloadEnd = Time.time + slot.Data.reloadTime + extraDelay;
            GameManager.Instance.Audio.Play2D(Sfx.Reload, 0.7f);
            Changed?.Invoke();
        }

        private void FinishReload()
        {
            var slot = Current;
            _reloading = false;
            if (slot == null) return;
            int needed = slot.Data.magazineSize - slot.Ammo;
            int taken = Mathf.Min(needed, slot.Reserve);
            slot.Ammo += taken;
            slot.Reserve -= taken;
            Changed?.Invoke();
        }

        private void UpdateViewModel(Slot slot, GameInput actions)
        {
            var data = slot.Data;
            Transform view = slot.View.transform;

            Vector2 look = InputGate.IsBlocked ? Vector2.zero : actions.Look.ReadValue<Vector2>();
            Vector3 targetSway = new Vector3(-look.x, -look.y, 0f) * 0.0006f * Mathf.Lerp(1f, 0.2f, _aim);
            targetSway = Vector3.ClampMagnitude(targetSway, 0.04f);
            _sway = Vector3.Lerp(_sway, targetSway, Time.deltaTime * 8f);

            float speed = motor != null ? motor.HorizontalSpeed : 0f;
            if (motor != null && motor.IsGrounded && speed > 0.5f) _bobPhase += Time.deltaTime * speed * 1.9f;
            float bobAmount = Mathf.Clamp01(speed / 7f) * 0.018f * Mathf.Lerp(1f, 0.15f, _aim);
            Vector3 bob = new Vector3(Mathf.Cos(_bobPhase) * bobAmount, Mathf.Abs(Mathf.Sin(_bobPhase)) * bobAmount, 0f);

            _kick = Vector3.Lerp(_kick, Vector3.zero, Time.deltaTime * 14f);

            Vector3 pos = Vector3.Lerp(data.viewOffset, data.aimOffset, _aim) + _sway + bob + _kick;
            float lowered = 1f - _switchBlend;
            float reloadDip = _reloading ? Mathf.Sin(ReloadProgress * Mathf.PI) : 0f;
            pos += new Vector3(0f, -0.25f * lowered - 0.08f * reloadDip, 0f);
            bool sprinting = motor != null && motor.IsSprinting;
            Quaternion rot = Quaternion.Euler(-_kick.z * 60f + reloadDip * 25f + lowered * 30f + (sprinting ? 12f : 0f),
                                              sprinting ? -25f : 0f, reloadDip * -20f);
            view.localPosition = Vector3.Lerp(view.localPosition, pos, Time.deltaTime * 20f);
            view.localRotation = Quaternion.Slerp(view.localRotation, rot, Time.deltaTime * 14f);

            // Hide the view model behind the scope overlay.
            bool hide = IsScoped;
            foreach (var r in slot.View.GetComponentsInChildren<Renderer>())
                if (r.enabled == hide) r.enabled = !hide;
        }
    }
}
