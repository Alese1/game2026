using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwordArt.Core
{
    /// <summary>
    /// Asynchronous scene transitions behind the loading screen.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField] private float minimumDisplayTime = 1.2f;

        /// <summary>True while a transition is running.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Raised when a load starts (target scene name).</summary>
        public event Action<string> LoadStarted;

        /// <summary>Raised after the new scene is active (scene name).</summary>
        public event Action<string> LoadFinished;

        /// <summary>
        /// Loads <paramref name="sceneName"/> and spawns the player at <paramref name="spawnId"/>.
        /// Ignored while another transition is running.
        /// </summary>
        public void LoadScene(string sceneName, string spawnId, string title)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName, spawnId, title));
        }

        private IEnumerator LoadRoutine(string sceneName, string spawnId, string title)
        {
            IsLoading = true;
            var gm = GameManager.Instance;
            InputGate.Block(this);
            LoadStarted?.Invoke(sceneName);

            var screen = gm.UI.Loading;
            bool virtualWorld = sceneName == SceneNames.GGO;
            yield return screen.Show(title, virtualWorld);

            Time.timeScale = 1f;
            gm.PendingSpawnId = spawnId;
            float started = Time.unscaledTime;

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");
                gm.UI.Notify($"Не удалось загрузить сцену {sceneName}", UI.NotificationKind.Error);
                yield return screen.Hide();
                InputGate.Unblock(this);
                IsLoading = false;
                yield break;
            }

            op.allowSceneActivation = false;
            while (op.progress < 0.9f || Time.unscaledTime - started < minimumDisplayTime)
            {
                float p = Mathf.Min(op.progress / 0.9f, (Time.unscaledTime - started) / minimumDisplayTime);
                screen.SetProgress(p);
                yield return null;
            }
            screen.SetProgress(1f);
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            InputGate.Clear();
            InputGate.Block(this);
            yield return null;
            yield return screen.Hide();
            InputGate.Unblock(this);
            IsLoading = false;
            LoadFinished?.Invoke(sceneName);
        }
    }
}
