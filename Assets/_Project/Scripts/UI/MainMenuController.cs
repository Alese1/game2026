using SwordArt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Start menu: New game, Continue (disabled without a save), Settings, Quit.
    /// Also slowly orbits the menu camera around the showcase object.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float orbitSpeed = 4f;

        private Button _continue;
        private Text _saveInfo;

        private void Start()
        {
            var gm = GameManager.Instance;
            var theme = UITheme.Real;
            var canvas = UIKit.CreateCanvas("MainMenuCanvas", 5, transform);

            var shade = UIKit.Image(canvas.transform, "Shade", new Color(0.01f, 0.02f, 0.05f, 0.55f));
            shade.rectTransform.Place(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            var title = UIKit.Text(canvas.transform, "Title", "SWORD ART ONLINE", 76, theme.Text, TextAnchor.LowerLeft, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -120), new Vector2(1200, 100));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
            var sub = UIKit.Text(canvas.transform, "Subtitle", "OPEN WORLD  ·  GUN GALE ONLINE", 30, theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(124, -225), new Vector2(1200, 44));
            var line = UIKit.Image(canvas.transform, "Line", theme.Accent);
            line.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(124, -280), new Vector2(420, 3));

            var list = UIKit.Rect(canvas.transform, "Buttons");
            list.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -60), new Vector2(420, 330));
            UIKit.VerticalLayout(list, 16, new RectOffset(0, 0, 0, 0));

            UIKit.Height(UIKit.Button(list, "Новая игра", theme, () => gm.NewGame(), 26), 64);
            _continue = UIKit.Button(list, "Продолжить", theme, () =>
            {
                if (!gm.ContinueGame()) gm.UI.Notify("Сохранение не найдено", NotificationKind.Error);
            }, 26);
            UIKit.Height(_continue, 64);
            UIKit.Height(UIKit.Button(list, "Настройки", theme, () => gm.UI.Settings.Open(), 26), 64);
            UIKit.Height(UIKit.Button(list, "Выход", theme, () => gm.QuitGame(), 26), 64);

            _saveInfo = UIKit.Text(canvas.transform, "SaveInfo", "", 20, theme.TextDim, TextAnchor.UpperLeft);
            _saveInfo.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(124, -240), new Vector2(700, 80));

            var footer = UIKit.Text(canvas.transform, "Footer", "Фанатский прототип · WASD — движение · E — взаимодействие · Esc — пауза", 18, theme.TextDim, TextAnchor.LowerLeft);
            footer.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(124, 36), new Vector2(-124, 30));

            RefreshContinue();
            InputGate.SetGameplayCursorLock(false);
        }

        private void RefreshContinue()
        {
            var gm = GameManager.Instance;
            bool hasSave = gm.SaveSystem.HasSave;
            _continue.interactable = hasSave;
            if (hasSave)
            {
                var data = gm.SaveSystem.Load(out _);
                _saveInfo.text = data != null
                    ? $"Сохранение: {data.lastSaveTime} · {data.credits} кр. · турниров: {data.progress.tournamentsPlayed}, побед: {data.progress.tournamentsWon}"
                    : "";
            }
            else _saveInfo.text = "Сохранений нет — начните новую игру.";
        }

        private void Update()
        {
            if (cameraPivot != null) cameraPivot.Rotate(0f, orbitSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
