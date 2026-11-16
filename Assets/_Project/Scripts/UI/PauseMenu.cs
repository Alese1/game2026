using SwordArt.Core;
using UnityEngine;

namespace SwordArt.UI
{
    /// <summary>Pause menu (Esc). Freezes time; options depend on the current world.</summary>
    public sealed class PauseMenu : UIWindow
    {
        private WorldType _builtFor;

        /// <summary>Raised when the player chooses "leave GGO" (handled by the GGO scene).</summary>
        public static event System.Action LogoutRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LogoutRequested = null;

        protected override void Build(UITheme theme)
        {
            var gm = GameManager.Instance;
            _builtFor = gm.CurrentWorld;
            bool virtualWorld = _builtFor == WorldType.Virtual;

            var panel = CreateDialogFrame(theme, new Vector2(520, virtualWorld ? 470 : 530), virtualWorld ? "ПАУЗА — GGO" : "ПАУЗА");
            var list = UIKit.Rect(panel, "Buttons");
            list.Place(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(-80, -130));
            UIKit.VerticalLayout(list, 14, new RectOffset(0, 0, 0, 0));

            UIKit.Height(UIKit.Button(list, "Продолжить", theme, Close), 60);
            UIKit.Height(UIKit.Button(list, "Настройки", theme, () => gm.UI.Settings.Open()), 60);
            if (virtualWorld)
            {
                UIKit.Height(UIKit.Button(list, "Выйти из GGO", theme, () =>
                {
                    Close();
                    if (LogoutRequested != null) LogoutRequested.Invoke();
                    else gm.SceneLoader.LoadScene(SceneNames.Apartment, SpawnIds.ApartmentFullDive, "Выход из FullDive");
                }), 60);
            }
            else
            {
                UIKit.Height(UIKit.Button(list, "Сохранить игру", theme, () =>
                {
                    gm.SaveGame("manual");
                    gm.UI.Notify("Игра сохранена", NotificationKind.Success);
                }), 60);
            }
            UIKit.Height(UIKit.Button(list, "Главное меню", theme, () =>
            {
                Close();
                gm.ReturnToMainMenu();
            }), 60);
            UIKit.Height(UIKit.Button(list, "Выйти из игры", theme, () => gm.QuitGame()), 60);
        }

        protected override void OnOpened()
        {
            if (GameManager.Instance.CurrentWorld != _builtFor) Rebuild(Theme);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        protected override void OnClosed()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
