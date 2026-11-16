using UnityEditor;
using UnityEditorInternal;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Development helper: keeps Play Mode ticking while the Editor window is not focused
    /// (needed for automated testing through MCP). In the background the editor does not run the
    /// player loop, so we pause and step frames from EditorApplication.update instead.
    /// Toggle via SwordArt/Dev/Keep Play Mode Alive. <see cref="StepFrames"/> advances frames directly.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeKeepAlive
    {
        private const string Key = "SwordArt.KeepPlayModeAlive";
        private const string MenuPath = "SwordArt/Dev/Keep Play Mode Alive";
        private static bool _autoPaused;

        static PlayModeKeepAlive()
        {
            EditorApplication.update += Tick;
        }

        private static bool Enabled => EditorPrefs.GetBool(Key, false);

        [MenuItem(MenuPath)]
        private static void Toggle() => EditorPrefs.SetBool(Key, !Enabled);

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        /// <summary>Advances Play Mode by <paramref name="frames"/> frames immediately.</summary>
        public static void StepFrames(int frames)
        {
            if (!EditorApplication.isPlaying) return;
            if (!EditorApplication.isPaused)
            {
                EditorApplication.isPaused = true;
                _autoPaused = true;
            }
            for (int i = 0; i < frames; i++) EditorApplication.Step();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                _autoPaused = false;
                return;
            }
            bool focused = InternalEditorUtility.isApplicationActive;
            if (focused)
            {
                if (_autoPaused)
                {
                    _autoPaused = false;
                    EditorApplication.isPaused = false;
                }
                return;
            }
            if (!Enabled) return;
            if (EditorApplication.isPaused && !_autoPaused) return; // paused by the user
            StepFrames(1);
        }
    }
}
