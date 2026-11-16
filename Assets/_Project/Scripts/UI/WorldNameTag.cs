using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Small billboard label above a character (name + optional role), visible only nearby.
    /// Builds its own world-space canvas at runtime.
    /// </summary>
    public sealed class WorldNameTag : MonoBehaviour
    {
        [SerializeField] private string title = "Name";
        [SerializeField] private string subtitle = "";
        [SerializeField] private Color color = new Color(0.85f, 0.95f, 1f);
        [SerializeField] private float height = 2.1f;
        [SerializeField] private float visibleDistance = 14f;

        private Transform _canvas;
        private CanvasGroup _group;
        private Text _title;

        /// <summary>Configures the tag (used by builders and bots).</summary>
        public void Configure(string label, string sub, Color tint, float yOffset, float distance)
        {
            title = label;
            subtitle = sub;
            color = tint;
            height = yOffset;
            visibleDistance = distance;
            if (_title != null) _title.text = Compose();
        }

        /// <summary>Changes the main label at runtime.</summary>
        public void SetText(string label, Color tint)
        {
            title = label;
            color = tint;
            if (_title != null)
            {
                _title.text = Compose();
                _title.color = tint;
            }
        }

        private string Compose() => string.IsNullOrEmpty(subtitle) ? title : $"{title}\n<size=18><color=#B8C4D0>{subtitle}</color></size>";

        private void Start()
        {
            var go = new GameObject("NameTag", typeof(RectTransform));
            _canvas = go.transform;
            _canvas.SetParent(transform, false);
            _canvas.localPosition = Vector3.up * height;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)_canvas).sizeDelta = new Vector2(400, 100);
            _canvas.localScale = Vector3.one * 0.005f;
            _group = go.AddComponent<CanvasGroup>();

            _title = UIKit.Text(_canvas, "Label", Compose(), 30, color, TextAnchor.LowerCenter, FontStyle.Bold);
            _title.rectTransform.Stretch();
            var shadow = _title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || _canvas == null) return;
            float d = Vector3.Distance(cam.transform.position, _canvas.position);
            _group.alpha = Mathf.Clamp01((visibleDistance - d) / 3f);
            _canvas.rotation = Quaternion.LookRotation(_canvas.position - cam.transform.position);
        }
    }
}
