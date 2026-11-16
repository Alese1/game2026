using SwordArt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>Settings: master volume, mouse sensitivity, invert Y.</summary>
    public sealed class SettingsWindow : UIWindow
    {
        protected override void Build(UITheme theme)
        {
            var panel = CreateDialogFrame(theme, new Vector2(720, 420), "НАСТРОЙКИ");
            var list = UIKit.Rect(panel, "List");
            list.Place(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(-80, -130));
            UIKit.VerticalLayout(list, 12, new RectOffset(0, 0, 0, 0));

            UIKit.Slider(list, "Громкость", theme, 0f, 1f, GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
            UIKit.Slider(list, "Чувствительность мыши", theme, 0.1f, 3f, GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v);

            Button invert = null;
            invert = UIKit.Button(list, InvertLabel(), theme, () =>
            {
                GameSettings.InvertY = !GameSettings.InvertY;
                UIKit.SetButtonLabel(invert, InvertLabel());
            }, 22);
            UIKit.Height(invert, 52);

            var close = UIKit.Button(panel, "Готово", theme, Close);
            ((RectTransform)close.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(260, 56));
        }

        private static string InvertLabel() => "Инверсия по вертикали: " + (GameSettings.InvertY ? "ВКЛ" : "ВЫКЛ");
    }
}
