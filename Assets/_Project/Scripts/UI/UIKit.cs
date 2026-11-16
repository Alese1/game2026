using System;
using SwordArt.Audio;
using SwordArt.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Factory for consistently styled uGUI elements. Sprites (rounded panels, rings, frames)
    /// are generated procedurally so the UI has a unified look without imported textures.
    /// </summary>
    public static class UIKit
    {
        private static Font _font;
        private static Sprite _rounded;
        private static Sprite _frame;
        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _softGlow;

        /// <summary>UI font (Segoe UI / Arial from the OS, falls back to Unity's built-in font).</summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 32); }
                catch (Exception) { _font = null; }
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>9-sliced rounded rectangle.</summary>
        public static Sprite Rounded => _rounded ??= MakeRounded(48, 12, false, 0);

        /// <summary>9-sliced rounded outline.</summary>
        public static Sprite Frame => _frame ??= MakeRounded(48, 12, true, 3);

        /// <summary>Filled circle.</summary>
        public static Sprite Circle => _circle ??= MakeCircle(128, 0f);

        /// <summary>Thin ring.</summary>
        public static Sprite Ring => _ring ??= MakeCircle(256, 0.06f);

        /// <summary>Radial soft glow (vignettes, light blobs).</summary>
        public static Sprite SoftGlow => _softGlow ??= MakeGlow(128);

        /// <summary>Creates a Screen Space Overlay canvas scaled for 1920x1080.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Creates an empty RectTransform child.</summary>
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Sets anchors, pivot, position and size in one call.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Anchors at a single point (e.g. (0.5,0.5) = centre).</summary>
        public static RectTransform PlaceAt(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
            => rt.Place(anchor, anchor, anchor, position, size);

        /// <summary>Stretches to fill the parent with optional padding.</summary>
        public static RectTransform Stretch(this RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        /// <summary>Creates an Image.</summary>
        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sprite != null && sliced)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Rounded panel with a subtle outline.</summary>
        public static Image Panel(Transform parent, string name, UITheme theme, bool strong = false)
        {
            var img = Image(parent, name, strong ? theme.PanelStrong : theme.Panel, Rounded);
            img.raycastTarget = true;
            var frame = Image(img.transform, "Outline", theme.Border, Frame);
            frame.rectTransform.Stretch();
            return img;
        }

        /// <summary>Creates a Text element.</summary>
        public static Text Text(Transform parent, string name, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        /// <summary>Creates a themed button with hover/click sounds.</summary>
        public static Button Button(Transform parent, string label, UITheme theme, Action onClick, int fontSize = 24)
        {
            var img = Image(parent, "Button_" + label, Color.white, Rounded);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            ApplyButtonTheme(button, theme);

            var accent = Image(img.transform, "Accent", theme.Accent, null, false);
            accent.rectTransform.Place(new Vector2(0, 0.2f), new Vector2(0, 0.8f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(3, 0));

            var text = Text(img.transform, "Label", label, fontSize, theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch(4);

            if (onClick != null) button.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<UIButtonSound>();
            return button;
        }

        /// <summary>Re-colours a button for <paramref name="theme"/>.</summary>
        public static void ApplyButtonTheme(Button button, UITheme theme)
        {
            var colors = button.colors;
            colors.normalColor = theme.ButtonNormal;
            colors.highlightedColor = theme.ButtonHover;
            colors.selectedColor = theme.ButtonNormal;
            colors.pressedColor = theme.ButtonPressed;
            colors.disabledColor = theme.ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        /// <summary>Sets the label of a button created by <see cref="Button"/>.</summary>
        public static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        /// <summary>Creates a labelled horizontal slider.</summary>
        public static Slider Slider(Transform parent, string label, UITheme theme, float min, float max, float value, Action<float> onChanged)
        {
            var row = Rect(parent, "Slider_" + label);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 56;

            var text = Text(row, "Label", label, 22, theme.Text);
            text.rectTransform.Place(new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            var valueText = Text(row, "Value", "", 20, theme.TextDim, TextAnchor.MiddleRight);
            valueText.rectTransform.Place(new Vector2(0.85f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);

            var sliderRt = Rect(row, "Slider");
            sliderRt.Place(new Vector2(0.4f, 0.5f), new Vector2(0.84f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 24));
            var bg = Image(sliderRt, "Background", theme.ButtonNormal, Rounded);
            bg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 8));

            var fillArea = Rect(sliderRt, "Fill Area");
            fillArea.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 8));
            var fill = Image(fillArea, "Fill", theme.Accent, Rounded);
            fill.rectTransform.Stretch();

            var handleArea = Rect(sliderRt, "Handle Slide Area");
            handleArea.Stretch();
            var handle = Image(handleArea, "Handle", theme.Text, Circle, false);
            handle.rectTransform.sizeDelta = new Vector2(24, 24);
            handle.raycastTarget = true;

            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            void UpdateLabel(float v) => valueText.text = max <= 1.001f ? Mathf.RoundToInt(v * 100) + "%" : v.ToString("0.00");
            UpdateLabel(value);
            slider.onValueChanged.AddListener(v =>
            {
                UpdateLabel(v);
                onChanged?.Invoke(v);
            });
            return slider;
        }

        /// <summary>Adds a vertical layout group to <paramref name="rt"/>.</summary>
        public static VerticalLayoutGroup VerticalLayout(RectTransform rt, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        /// <summary>Adds a LayoutElement with a preferred height.</summary>
        public static LayoutElement Height(Component c, float height)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return le;
        }

        private static Sprite MakeRounded(int size, int radius, bool outline, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Signed distance to a rounded box (negative inside).
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var q = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                float d = Vector2.Distance(p, q);
                float ex = Mathf.Min(p.x - radius, size - radius - p.x);
                float ey = Mathf.Min(p.y - radius, size - radius - p.y);
                float sdf = d > 0f ? d - radius : -(radius + Mathf.Min(ex, ey));
                float a = Mathf.Clamp01(0.5f - sdf);
                if (outline) a *= Mathf.Clamp01(thickness + sdf + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            int b = radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private static Sprite MakeCircle(int size, float ringThickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                if (ringThickness > 0f) a *= Mathf.Clamp01(d - (r - ringThickness * size) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private static Sprite MakeGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }
    }

    /// <summary>Plays hover/click sounds for a button.</summary>
    public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            var b = GetComponent<Button>();
            if (b != null && b.interactable) GameManager.Instance?.Audio?.Play2D(Sfx.UIHover, 0.5f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var b = GetComponent<Button>();
            GameManager.Instance?.Audio?.Play2D(b != null && b.interactable ? Sfx.UIClick : Sfx.UIError, 0.7f);
        }
    }
}
