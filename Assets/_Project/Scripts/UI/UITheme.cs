using UnityEngine;

namespace SwordArt.UI
{
    /// <summary>Kind of on-screen notification; selects accent colour and sound.</summary>
    public enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Error,
        Credits
    }

    /// <summary>
    /// Colour palette for UI. The real world uses a calm "smart-glass" blue palette,
    /// GGO uses a high-contrast amber/teal military HUD palette.
    /// </summary>
    public sealed class UITheme
    {
        public Color Panel;
        public Color PanelStrong;
        public Color Border;
        public Color Accent;
        public Color Secondary;
        public Color Text;
        public Color TextDim;
        public Color Positive;
        public Color Negative;
        public Color Warning;
        public Color ButtonNormal;
        public Color ButtonHover;
        public Color ButtonPressed;
        public Color ButtonDisabled;

        /// <summary>Real-world palette.</summary>
        public static readonly UITheme Real = new UITheme
        {
            Panel = new Color(0.05f, 0.07f, 0.11f, 0.86f),
            PanelStrong = new Color(0.04f, 0.05f, 0.08f, 0.96f),
            Border = new Color(0.45f, 0.75f, 1f, 0.35f),
            Accent = new Color(0.36f, 0.78f, 1f, 1f),
            Secondary = new Color(1f, 0.62f, 0.78f, 1f),
            Text = new Color(0.93f, 0.96f, 1f, 1f),
            TextDim = new Color(0.62f, 0.7f, 0.8f, 1f),
            Positive = new Color(0.45f, 0.95f, 0.6f, 1f),
            Negative = new Color(1f, 0.42f, 0.42f, 1f),
            Warning = new Color(1f, 0.8f, 0.35f, 1f),
            ButtonNormal = new Color(0.12f, 0.17f, 0.25f, 0.95f),
            ButtonHover = new Color(0.2f, 0.36f, 0.52f, 1f),
            ButtonPressed = new Color(0.28f, 0.55f, 0.8f, 1f),
            ButtonDisabled = new Color(0.08f, 0.09f, 0.11f, 0.35f)
        };

        /// <summary>Gun Gale Online palette.</summary>
        public static readonly UITheme Virtual = new UITheme
        {
            Panel = new Color(0.06f, 0.05f, 0.03f, 0.82f),
            PanelStrong = new Color(0.04f, 0.035f, 0.02f, 0.95f),
            Border = new Color(1f, 0.6f, 0.2f, 0.4f),
            Accent = new Color(1f, 0.6f, 0.16f, 1f),
            Secondary = new Color(0.3f, 0.95f, 0.85f, 1f),
            Text = new Color(1f, 0.95f, 0.86f, 1f),
            TextDim = new Color(0.75f, 0.68f, 0.56f, 1f),
            Positive = new Color(0.4f, 1f, 0.55f, 1f),
            Negative = new Color(1f, 0.3f, 0.25f, 1f),
            Warning = new Color(1f, 0.85f, 0.2f, 1f),
            ButtonNormal = new Color(0.16f, 0.12f, 0.07f, 0.95f),
            ButtonHover = new Color(0.45f, 0.28f, 0.08f, 1f),
            ButtonPressed = new Color(0.75f, 0.45f, 0.1f, 1f),
            ButtonDisabled = new Color(0.1f, 0.08f, 0.06f, 0.35f)
        };

        /// <summary>Accent colour for a notification kind.</summary>
        public Color ForKind(NotificationKind kind) => kind switch
        {
            NotificationKind.Success => Positive,
            NotificationKind.Warning => Warning,
            NotificationKind.Error => Negative,
            NotificationKind.Credits => new Color(1f, 0.84f, 0.3f, 1f),
            _ => Accent
        };
    }
}
