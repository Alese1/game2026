using System.Collections.Generic;
using SwordArt.Core;
using UnityEngine;

namespace SwordArt.UI
{
    /// <summary>
    /// Owns the persistent UI (loading screen, notifications, interaction prompt, credits HUD and
    /// shared windows), the window stack, Esc handling and the active theme.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        private readonly List<UIWindow> _openWindows = new List<UIWindow>();
        private Canvas _hudCanvas;
        private Canvas _windowCanvas;
        private Canvas _overlayCanvas;

        public UITheme Theme { get; private set; } = UITheme.Real;
        public LoadingScreen Loading { get; private set; }
        public NotificationFeed Notifications { get; private set; }
        public InteractionPrompt Prompt { get; private set; }
        public CurrencyHUD CurrencyHud { get; private set; }
        public PauseMenu Pause { get; private set; }
        public SettingsWindow Settings { get; private set; }
        public DialogueWindow Dialogue { get; private set; }
        public ShopWindow Shop { get; private set; }
        public JobWindow Job { get; private set; }

        /// <summary>Canvas for scene-specific HUDs (GGO).</summary>
        public Canvas HudCanvas => _hudCanvas;

        /// <summary>Canvas for modal windows.</summary>
        public Canvas WindowCanvas => _windowCanvas;

        /// <summary>True while any window is open.</summary>
        public bool AnyWindowOpen => _openWindows.Count > 0;

        private void Awake()
        {
            var root = new GameObject("UI").transform;
            root.SetParent(transform, false);
            _hudCanvas = UIKit.CreateCanvas("HUDCanvas", 10, root);
            _windowCanvas = UIKit.CreateCanvas("WindowCanvas", 50, root);
            _overlayCanvas = UIKit.CreateCanvas("OverlayCanvas", 100, root);

            Prompt = CreateElement<InteractionPrompt>(_hudCanvas.transform, "InteractionPrompt");
            CurrencyHud = CreateElement<CurrencyHUD>(_hudCanvas.transform, "CurrencyHUD");
            Notifications = CreateElement<NotificationFeed>(_overlayCanvas.transform, "Notifications");
            Loading = CreateElement<LoadingScreen>(_overlayCanvas.transform, "LoadingScreen");
            Loading.transform.SetAsFirstSibling();

            Dialogue = CreateWindow<DialogueWindow>("DialogueWindow");
            Shop = CreateWindow<ShopWindow>("ShopWindow");
            Job = CreateWindow<JobWindow>("JobWindow");
            Pause = CreateWindow<PauseMenu>("PauseMenu");
            Settings = CreateWindow<SettingsWindow>("SettingsWindow");

            GameManager.Instance.WorldChanged += OnWorldChanged;
            OnWorldChanged(WorldType.Menu);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Input == null) return;
            if (gm.Input.Pause.WasPressedThisFrame()) HandleEscape();
        }

        /// <summary>Shows a toast notification.</summary>
        public void Notify(string message, NotificationKind kind = NotificationKind.Info)
        {
            if (Notifications != null) Notifications.Push(message, kind);
        }

        /// <summary>Creates a window of type <typeparamref name="T"/> on the window canvas (used by scenes too).</summary>
        public T CreateWindow<T>(string name) where T : UIWindow
        {
            var rt = UIKit.Rect(_windowCanvas.transform, name);
            var window = rt.gameObject.AddComponent<T>();
            window.Initialize();
            return window;
        }

        internal void RegisterOpen(UIWindow window)
        {
            _openWindows.Remove(window);
            _openWindows.Add(window);
        }

        internal void RegisterClosed(UIWindow window) => _openWindows.Remove(window);

        /// <summary>Closes all open windows (used on scene transitions).</summary>
        public void CloseAll()
        {
            for (int i = _openWindows.Count - 1; i >= 0; i--)
                if (i < _openWindows.Count && _openWindows[i] != null) _openWindows[i].Close();
            _openWindows.Clear();
        }

        private void HandleEscape()
        {
            var gm = GameManager.Instance;
            if (gm.SceneLoader != null && gm.SceneLoader.IsLoading) return;
            for (int i = _openWindows.Count - 1; i >= 0; i--)
            {
                var top = _openWindows[i];
                if (top == null) { _openWindows.RemoveAt(i); continue; }
                if (top.CloseOnEscape) top.Close();
                return;
            }
            if (gm.CurrentWorld != WorldType.Menu) Pause.Open();
        }

        private void OnWorldChanged(WorldType world)
        {
            CloseAll();
            Theme = world == WorldType.Virtual ? UITheme.Virtual : UITheme.Real;
            Prompt.ApplyTheme(Theme);
            Prompt.Hide();
            Notifications.ApplyTheme(Theme);
            CurrencyHud.SetVisible(world == WorldType.RealWorld);
            GameManager.Instance.Audio?.SetAmbience(world == WorldType.Virtual ? 2 : world == WorldType.RealWorld ? 1 : 0);
        }

        private static T CreateElement<T>(Transform parent, string name) where T : Component
        {
            var rt = UIKit.Rect(parent, name);
            rt.Stretch();
            return rt.gameObject.AddComponent<T>();
        }
    }
}
