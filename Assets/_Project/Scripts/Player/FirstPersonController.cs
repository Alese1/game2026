using SwordArt.Core;
using UnityEngine;

namespace SwordArt.Player
{
    /// <summary>
    /// GGO first-person movement and mouse look on a CharacterController. Supports locking
    /// (tournament countdown), recoil kicks, head bob and speed modifiers from the held weapon.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour, ITeleportListener
    {
        [Header("References")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float acceleration = 30f;
        [SerializeField] private float airControl = 0.35f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -20f;

        [Header("Look")]
        [SerializeField] private float sensitivity = 0.1f;
        [SerializeField] private float pitchLimit = 85f;
        [SerializeField] private float recoilRecovery = 9f;
        [SerializeField] private float bobAmount = 0.035f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private Vector2 _recoil;
        private float _bobPhase;
        private Vector3 _cameraRest;

        /// <summary>Prevents movement and looking (countdown, death).</summary>
        public bool MovementLocked { get; set; }

        /// <summary>Prevents looking around (death).</summary>
        public bool LookLocked { get; set; }

        /// <summary>Speed multiplier from weapon mobility / aiming.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>When true sprinting is suppressed (aiming, firing).</summary>
        public bool SprintBlocked { get; set; }

        public Camera Camera => playerCamera;
        public float HorizontalSpeed => new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        public float RunSpeed => runSpeed;
        public bool IsGrounded => _controller.isGrounded;
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;
            if (cameraRoot != null) _cameraRest = cameraRoot.localPosition;
        }

        private void OnEnable() => InputGate.SetGameplayCursorLock(true);

        private void OnDisable() => InputGate.SetGameplayCursorLock(false);

        /// <summary>Adds a recoil kick (degrees up, degrees sideways).</summary>
        public void AddRecoil(float up, float side) => _recoil += new Vector2(side, up);

        private void Update()
        {
            var gm = GameManager.Instance;
            bool inputAllowed = gm != null && !InputGate.IsBlocked;
            bool canMove = inputAllowed && !MovementLocked;

            if (inputAllowed && !LookLocked)
            {
                Vector2 look = gm.Input.Look.ReadValue<Vector2>();
                float fovScale = playerCamera != null ? playerCamera.fieldOfView / 70f : 1f;
                float s = sensitivity * GameSettings.MouseSensitivity * fovScale;
                _yaw += look.x * s;
                _pitch += (GameSettings.InvertY ? look.y : -look.y) * s;
                _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);
            }

            // Recoil is applied as an offset that recovers over time; part of it is permanent.
            Vector2 recovered = Vector2.Lerp(_recoil, Vector2.zero, Time.deltaTime * recoilRecovery);
            Vector2 applied = _recoil - recovered;
            _recoil = recovered;
            _pitch = Mathf.Clamp(_pitch - applied.y * 0.35f, -pitchLimit, pitchLimit);
            _yaw += applied.x * 0.35f;

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(_pitch - _recoil.y, _recoil.x, 0f);

            Vector2 move = canMove ? gm.Input.Move.ReadValue<Vector2>() : Vector2.zero;
            bool wantsSprint = canMove && gm.Input.Sprint.IsPressed() && move.y > 0.1f && !SprintBlocked;
            IsSprinting = wantsSprint && _controller.isGrounded;
            float speed = (wantsSprint ? runSpeed : walkSpeed) * SpeedMultiplier;
            Vector3 desired = transform.TransformDirection(Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f)) * speed;

            float control = _controller.isGrounded ? 1f : airControl;
            Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, desired, acceleration * control * Time.deltaTime);
            _velocity.x = horizontal.x;
            _velocity.z = horizontal.z;

            if (_controller.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (canMove && gm.Input.Jump.WasPressedThisFrame()) _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _velocity.y = Mathf.Max(_velocity.y + gravity * Time.deltaTime, -50f);
            _controller.Move(_velocity * Time.deltaTime);

            UpdateBob(horizontal.magnitude);
        }

        private void UpdateBob(float speed)
        {
            if (cameraRoot == null) return;
            if (_controller.isGrounded && speed > 0.5f) _bobPhase += Time.deltaTime * speed * 1.9f;
            float amount = _controller.isGrounded ? Mathf.Clamp01(speed / runSpeed) * bobAmount : 0f;
            Vector3 offset = new Vector3(Mathf.Cos(_bobPhase) * amount * 0.5f, Mathf.Abs(Mathf.Sin(_bobPhase)) * amount, 0f);
            cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, _cameraRest + offset, Time.deltaTime * 12f);
        }

        /// <summary>Points the view at a world direction (used after spawning).</summary>
        public void SetLook(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
            _recoil = Vector2.zero;
        }

        public void OnTeleported()
        {
            _velocity = Vector3.zero;
            SetLook(transform.eulerAngles.y, 0f);
        }
    }
}
