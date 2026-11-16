using SwordArt.Core;
using UnityEngine;

namespace SwordArt.Player
{
    /// <summary>
    /// Mouse-driven orbit target for the Cinemachine third-person camera. Rotates a camera
    /// target transform in world space (independent of the character's facing) and clamps pitch.
    /// </summary>
    public sealed class ThirdPersonCameraRig : MonoBehaviour, ITeleportListener
    {
        [Tooltip("Transform followed by the CinemachineCamera (ThirdPersonFollow).")]
        [SerializeField] private Transform cameraTarget;
        [Tooltip("Character root (the target is positioned relative to it).")]
        [SerializeField] private Transform character;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;

        private float _yaw;
        private float _pitch = 10f;

        /// <summary>Current camera yaw in degrees (used for camera-relative movement).</summary>
        public float Yaw => _yaw;

        public Transform CameraTarget => cameraTarget;

        private void Start() => _yaw = character != null ? character.eulerAngles.y : 0f;

        private void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (gm != null && !InputGate.IsBlocked)
            {
                Vector2 look = gm.Input.Look.ReadValue<Vector2>();
                float s = sensitivity * GameSettings.MouseSensitivity;
                _yaw += look.x * s;
                _pitch += (GameSettings.InvertY ? look.y : -look.y) * s;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }
            if (cameraTarget == null || character == null) return;
            cameraTarget.position = character.position + targetOffset;
            cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>Sets the orbit angles directly (used by tests and cut-ins).</summary>
        public void SetAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        public void OnTeleported()
        {
            if (character != null) _yaw = character.eulerAngles.y;
            _pitch = 10f;
        }
    }
}
