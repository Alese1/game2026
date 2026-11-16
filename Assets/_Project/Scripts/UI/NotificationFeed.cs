using System.Collections.Generic;
using SwordArt.Audio;
using SwordArt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>Stack of short toast messages in the top-right corner.</summary>
    public sealed class NotificationFeed : MonoBehaviour
    {
        private const int MaxVisible = 5;
        private const float Lifetime = 3.8f;

        private sealed class Toast
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float Age;
        }

        private readonly List<Toast> _toasts = new List<Toast>();
        private RectTransform _column;
        private UITheme _theme = UITheme.Real;

        private void Awake()
        {
            _column = UIKit.Rect(transform, "Column");
            _column.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -110), new Vector2(460, 600));
            var layout = UIKit.VerticalLayout(_column, 8, new RectOffset(0, 0, 0, 0), TextAnchor.UpperRight);
            layout.childForceExpandWidth = true;
        }

        /// <summary>Switches the colour palette.</summary>
        public void ApplyTheme(UITheme theme) => _theme = theme;

        /// <summary>Adds a toast.</summary>
        public void Push(string message, NotificationKind kind)
        {
            var bg = UIKit.Panel(_column, "Toast", _theme);
            bg.raycastTarget = false;
            UIKit.Height(bg, 58);
            var accent = UIKit.Image(bg.transform, "Accent", _theme.ForKind(kind), UIKit.Rounded);
            accent.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(5, -18));
            var text = UIKit.Text(bg.transform, "Text", message, 21, _theme.Text);
            text.rectTransform.Stretch();
            text.rectTransform.offsetMin = new Vector2(26, 4);
            text.rectTransform.offsetMax = new Vector2(-14, -4);
            var group = bg.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            _toasts.Add(new Toast { Rect = bg.rectTransform, Group = group });

            while (_toasts.Count > MaxVisible)
            {
                Destroy(_toasts[0].Rect.gameObject);
                _toasts.RemoveAt(0);
            }

            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            if (audio != null)
            {
                if (kind == NotificationKind.Credits) audio.Play2D(Sfx.Coin, 0.6f);
                else if (kind == NotificationKind.Error) audio.Play2D(Sfx.UIError, 0.5f);
            }
        }

        private void Update()
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var t = _toasts[i];
                t.Age += Time.unscaledDeltaTime;
                t.Group.alpha = t.Age < 0.2f ? t.Age / 0.2f : Mathf.Clamp01((Lifetime - t.Age) / 0.5f);
                if (t.Age >= Lifetime)
                {
                    Destroy(t.Rect.gameObject);
                    _toasts.RemoveAt(i);
                }
            }
        }
    }
}
