using SwordArt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>Small credits indicator (top-left) with an animated counter and delta popup.</summary>
    public sealed class CurrencyHUD : MonoBehaviour
    {
        private RectTransform _root;
        private Text _amount;
        private Text _delta;
        private CanvasGroup _group;
        private float _displayed;
        private int _target;
        private float _deltaTimer;
        private bool _visible;

        private void Awake()
        {
            var theme = UITheme.Real;
            _root = UIKit.Rect(transform, "Credits");
            _root.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), new Vector2(250, 54));
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            var panel = UIKit.Panel(_root, "Panel", theme);
            panel.raycastTarget = false;
            panel.rectTransform.Stretch();

            var coin = UIKit.Image(_root, "Coin", new Color(1f, 0.82f, 0.3f), UIKit.Circle, false);
            coin.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(30, 30));
            var c = UIKit.Text(coin.transform, "C", "C", 20, new Color(0.35f, 0.25f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold);
            c.rectTransform.Stretch();

            _amount = UIKit.Text(_root, "Amount", "0", 28, theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _amount.rectTransform.Stretch();
            _amount.rectTransform.offsetMin = new Vector2(56, 0);

            _delta = UIKit.Text(_root, "Delta", "", 24, theme.Positive, TextAnchor.MiddleLeft, FontStyle.Bold);
            _delta.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(56, -4), new Vector2(0, 34));
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            gm.Currency.CreditsChanged += OnCreditsChanged;
            _target = gm.Currency.Credits;
            _displayed = _target;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Currency != null) gm.Currency.CreditsChanged -= OnCreditsChanged;
        }

        /// <summary>Shows or hides the indicator.</summary>
        public void SetVisible(bool visible) => _visible = visible;

        private void OnCreditsChanged(int balance, int delta, string reason)
        {
            _target = balance;
            if (delta == 0)
            {
                _displayed = balance;
                return;
            }
            _delta.text = (delta > 0 ? "+" : "") + delta + " кр.";
            _delta.color = delta > 0 ? UITheme.Real.Positive : UITheme.Real.Negative;
            _deltaTimer = 2.2f;
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            _displayed = Mathf.MoveTowards(_displayed, _target, Mathf.Max(1f, Mathf.Abs(_target - _displayed)) * Time.unscaledDeltaTime * 6f);
            _amount.text = Mathf.RoundToInt(_displayed).ToString("N0") + " кр.";
            _deltaTimer -= Time.unscaledDeltaTime;
            var col = _delta.color;
            col.a = Mathf.Clamp01(_deltaTimer);
            _delta.color = col;
        }
    }
}
