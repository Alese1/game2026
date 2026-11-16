using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>Context hint "[E] Action" shown while aiming at an interactable.</summary>
    public sealed class InteractionPrompt : MonoBehaviour
    {
        private RectTransform _root;
        private Image _panel;
        private Image _outline;
        private Image _keyCap;
        private Text _keyText;
        private Text _label;
        private CanvasGroup _group;
        private float _pulse;
        private bool _visible;

        private void Awake()
        {
            _root = UIKit.Rect(transform, "Prompt");
            _root.PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(520, 60));
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            _panel = UIKit.Image(_root, "Panel", Color.black, UIKit.Rounded);
            _panel.rectTransform.Stretch();
            _outline = UIKit.Image(_panel.transform, "Outline", Color.white, UIKit.Frame);
            _outline.rectTransform.Stretch();

            _keyCap = UIKit.Image(_root, "KeyCap", Color.white, UIKit.Rounded);
            _keyCap.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(40, 40));
            _keyText = UIKit.Text(_keyCap.transform, "Key", "E", 24, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            _keyText.rectTransform.Stretch();

            _label = UIKit.Text(_root, "Label", "", 24, Color.white);
            _label.rectTransform.Stretch();
            _label.rectTransform.offsetMin = new Vector2(66, 0);
            _label.rectTransform.offsetMax = new Vector2(-14, 0);
            ApplyTheme(UITheme.Real);
        }

        /// <summary>Re-colours the prompt.</summary>
        public void ApplyTheme(UITheme theme)
        {
            if (_panel == null) return;
            _panel.color = theme.Panel;
            _outline.color = theme.Border;
            _keyCap.color = theme.Accent;
            _keyText.color = theme.PanelStrong;
            _label.color = theme.Text;
        }

        /// <summary>Shows the prompt. <paramref name="text"/> may start with "E — ", which is stripped.</summary>
        public void Show(string text)
        {
            if (text.StartsWith("E — ")) text = text.Substring(4);
            _label.text = text;
            float width = Mathf.Clamp(_label.preferredWidth + 96f, 220f, 900f);
            _root.sizeDelta = new Vector2(width, 60);
            _visible = true;
        }

        /// <summary>Hides the prompt.</summary>
        public void Hide() => _visible = false;

        /// <summary>Short scale pulse used as interaction feedback.</summary>
        public void Pulse() => _pulse = 1f;

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, Time.unscaledDeltaTime * 10f);
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * 5f);
            _root.localScale = Vector3.one * (1f + _pulse * 0.08f);
        }
    }
}
