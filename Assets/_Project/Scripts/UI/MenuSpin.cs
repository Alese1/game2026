using UnityEngine;

namespace SwordArt.UI
{
    /// <summary>Slowly spins and bobs a showcase object (main menu).</summary>
    public sealed class MenuSpin : MonoBehaviour
    {
        public float speed = 10f;
        public float bob = 0.03f;

        private Vector3 _start;

        private void Start() => _start = transform.localPosition;

        private void Update()
        {
            transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
            transform.localPosition = _start + Vector3.up * Mathf.Sin(Time.time * 1.3f) * bob;
        }
    }
}
