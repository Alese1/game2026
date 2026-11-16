using UnityEngine;

namespace SwordArt.Player
{
    /// <summary>
    /// Lightweight procedural animation for the segmented low-poly characters: swings arms and
    /// legs with speed, adds body bob, idle breathing, an optional "weapon ready" arm pose and a
    /// fall-over death pose. Limb pivots are found by name (Arm_L, Arm_R, Leg_L, Leg_R, Torso, Head).
    /// </summary>
    public sealed class ProceduralCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private float strideFrequency = 1.6f;
        [SerializeField] private float maxSwing = 38f;
        [Tooltip("Holds both arms forward as if carrying a rifle.")]
        [SerializeField] private bool weaponPose;

        private Transform _armL, _armR, _legL, _legR, _torso, _head;
        private Quaternion _armLRest, _armRRest, _legLRest, _legRRest, _torsoRest, _headRest;
        private Vector3 _modelRest;
        private Vector3 _lastPosition;
        private float _phase;
        private float _speed;
        private float _deathBlend;

        /// <summary>Overrides the measured speed (m/s). Negative = measure from movement.</summary>
        public float SpeedOverride { get; set; } = -1f;

        /// <summary>Falls over when true.</summary>
        public bool Dead { get; set; }

        /// <summary>Arms held forward (armed characters).</summary>
        public bool WeaponPose
        {
            get => weaponPose;
            set => weaponPose = value;
        }

        /// <summary>Configures the model root (used by builders).</summary>
        public void Configure(Transform modelRoot, bool armed)
        {
            model = modelRoot;
            weaponPose = armed;
        }

        private void Awake()
        {
            if (model == null) model = transform;
            _armL = Find("Arm_L");
            _armR = Find("Arm_R");
            _legL = Find("Leg_L");
            _legR = Find("Leg_R");
            _torso = Find("Torso");
            _head = Find("Head");
            if (_armL) _armLRest = _armL.localRotation;
            if (_armR) _armRRest = _armR.localRotation;
            if (_legL) _legLRest = _legL.localRotation;
            if (_legR) _legRRest = _legR.localRotation;
            if (_torso) _torsoRest = _torso.localRotation;
            if (_head) _headRest = _head.localRotation;
            _modelRest = model.localPosition;
            _lastPosition = transform.position;
        }

        private Transform Find(string partName)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                if (t.name == partName) return t;
            return null;
        }

        private void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            delta.y = 0f;
            float measured = delta.magnitude / dt;
            float target = SpeedOverride >= 0f ? SpeedOverride : measured;
            _speed = Mathf.Lerp(_speed, Mathf.Min(target, 8f), dt * 10f);

            float speed01 = Mathf.Clamp01(_speed / 5.5f);
            if (_speed > 0.15f) _phase += dt * strideFrequency * Mathf.PI * 2f * Mathf.Lerp(0.6f, 1.7f, speed01);
            float amount = Mathf.Clamp01(_speed / 2f);
            float swing = Mathf.Sin(_phase) * maxSwing * amount * Mathf.Lerp(0.8f, 1.3f, speed01);
            float breath = Mathf.Sin(Time.time * 1.7f) * 1.5f;

            _deathBlend = Mathf.MoveTowards(_deathBlend, Dead ? 1f : 0f, dt * 2.5f);
            if (Dead) swing = 0f;

            if (_legL) _legL.localRotation = _legLRest * Quaternion.Euler(swing, 0f, 0f);
            if (_legR) _legR.localRotation = _legRRest * Quaternion.Euler(-swing, 0f, 0f);
            if (weaponPose && !Dead)
            {
                if (_armL) _armL.localRotation = _armLRest * Quaternion.Euler(-70f + breath, 0f, 25f);
                if (_armR) _armR.localRotation = _armRRest * Quaternion.Euler(-80f + breath, 0f, -10f);
            }
            else
            {
                // Left arm sits at -X: negative Z roll moves it away from the body.
                if (_armL) _armL.localRotation = _armLRest * Quaternion.Euler(-swing * 0.8f, 0f, -5f - breath * 0.5f);
                if (_armR) _armR.localRotation = _armRRest * Quaternion.Euler(swing * 0.8f, 0f, 5f + breath * 0.5f);
            }
            if (_torso) _torso.localRotation = _torsoRest * Quaternion.Euler(breath * 0.4f + speed01 * 6f, Mathf.Sin(_phase) * 4f * speed01, 0f);
            if (_head) _head.localRotation = _headRest * Quaternion.Euler(-speed01 * 4f, 0f, 0f);

            float bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.05f * amount;
            model.localPosition = _modelRest + Vector3.up * bob;
            model.localRotation = _deathBlend > 0f
                ? Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-85f, 0f, 0f), Mathf.SmoothStep(0f, 1f, _deathBlend))
                : Quaternion.identity;
        }
    }
}
