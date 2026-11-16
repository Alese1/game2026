using SwordArt.Core;
using UnityEngine;

namespace SwordArt.Player
{
    /// <summary>
    /// Real-world third-person movement on a CharacterController: camera-relative walking,
    /// sprinting, jumping, gravity, ground check and smooth rotation toward the move direction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour, ITeleportListener
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.3f;
        [SerializeField] private float runSpeed = 5.6f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Header("Jump & gravity")]
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private float groundCheckOffset = 0.1f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("References")]
        [Tooltip("Yaw source for camera-relative movement (the camera rig).")]
        [SerializeField] private ThirdPersonCameraRig cameraRig;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private bool _grounded;

        /// <summary>Horizontal speed in m/s (for animation).</summary>
        public float Speed => _horizontalVelocity.magnitude;

        /// <summary>True when standing on ground.</summary>
        public bool IsGrounded => _grounded;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void OnEnable() => InputGate.SetGameplayCursorLock(true);

        private void OnDisable() => InputGate.SetGameplayCursorLock(false);

        private void Update()
        {
            var gm = GameManager.Instance;
            bool canControl = gm != null && !InputGate.IsBlocked;
            Vector2 move = canControl ? gm.Input.Move.ReadValue<Vector2>() : Vector2.zero;
            bool sprint = canControl && gm.Input.Sprint.IsPressed();
            bool jump = canControl && gm.Input.Jump.WasPressedThisFrame();

            CheckGround();

            float yaw = cameraRig != null ? cameraRig.Yaw : transform.eulerAngles.y;
            Vector3 input = Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);
            Vector3 desired = Quaternion.Euler(0f, yaw, 0f) * input * (sprint ? runSpeed : walkSpeed);
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desired, acceleration * Time.deltaTime);

            if (input.sqrMagnitude > 0.01f)
            {
                float targetYaw = Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg;
                float smoothed = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothed, 0f);
            }

            if (_grounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (jump) _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * Time.deltaTime, -50f);

            _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        private void CheckGround()
        {
            Vector3 p = transform.position + Vector3.up * (groundCheckRadius - groundCheckOffset);
            bool sphere = Physics.CheckSphere(p, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            _grounded = _controller.isGrounded || (sphere && _verticalVelocity <= 0.1f);
        }

        public void OnTeleported()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }
    }
}
