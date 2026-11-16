using SwordArt.Core;
using UnityEngine;

namespace SwordArt.UI
{
    /// <summary>
    /// Base class for modal windows built in code. Opening a window blocks gameplay input,
    /// unlocks the cursor and pushes the window on the <see cref="UIManager"/> stack so Esc closes it.
    /// The content is (re)built lazily whenever the active <see cref="UITheme"/> changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIWindow : MonoBehaviour
    {
        private CanvasGroup _group;
        private UITheme _builtTheme;
        private int _openedFrame;

        /// <summary>True while the window is visible.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Whether Esc closes this window.</summary>
        public virtual bool CloseOnEscape => true;

        /// <summary>Theme used for the current content.</summary>
        protected UITheme Theme => _builtTheme;

        /// <summary>False during the frame the window was opened (avoids reusing the opening key press).</summary>
        protected bool InputFresh => Time.frameCount > _openedFrame;

        /// <summary>Builds the window content for <paramref name="theme"/> under this transform.</summary>
        protected abstract void Build(UITheme theme);

        /// <summary>Called after the window becomes visible.</summary>
        protected virtual void OnOpened() { }

        /// <summary>Called after the window was hidden.</summary>
        protected virtual void OnClosed() { }

        /// <summary>Prepares the window (hidden).</summary>
        public void Initialize()
        {
            if (!TryGetComponent(out _group)) _group = gameObject.AddComponent<CanvasGroup>();
            ((RectTransform)transform).Stretch();
            gameObject.SetActive(false);
        }

        /// <summary>Shows the window.</summary>
        public void Open()
        {
            if (IsOpen) return;
            if (_group == null) Initialize();
            var ui = GameManager.Instance != null ? GameManager.Instance.UI : null;
            var theme = ui != null ? ui.Theme : UITheme.Real;
            if (_builtTheme != theme) Rebuild(theme);

            IsOpen = true;
            _openedFrame = Time.frameCount;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _group.alpha = 0f;
            InputGate.Block(this);
            ui?.RegisterOpen(this);
            OnOpened();
        }

        /// <summary>Hides the window.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            InputGate.Unblock(this);
            GameManager.Instance?.UI?.RegisterClosed(this);
            gameObject.SetActive(false);
            OnClosed();
        }

        /// <summary>Forces a rebuild with <paramref name="theme"/>.</summary>
        protected void Rebuild(UITheme theme)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _builtTheme = theme;
            Build(theme);
        }

        protected virtual void Update()
        {
            if (IsOpen && _group.alpha < 1f) _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, Time.unscaledDeltaTime * 8f);
        }

        /// <summary>Creates a dimmed full-screen backdrop and a centred themed panel.</summary>
        protected RectTransform CreateDialogFrame(UITheme theme, Vector2 size, string title, bool dim = true)
        {
            if (dim)
            {
                var backdrop = UIKit.Image(transform, "Backdrop", new Color(0f, 0f, 0f, 0.45f));
                backdrop.rectTransform.Stretch();
                backdrop.raycastTarget = true;
            }
            var panel = UIKit.Panel(transform, "Panel", theme, true);
            panel.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (!string.IsNullOrEmpty(title))
            {
                var t = UIKit.Text(panel.transform, "Title", title, 34, theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(-64, 52));
                var line = UIKit.Image(panel.transform, "TitleLine", theme.Border);
                line.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(-64, 2));
            }
            return panel.rectTransform;
        }
    }
}
