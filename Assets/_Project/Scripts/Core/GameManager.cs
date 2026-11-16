using System;
using SwordArt.Audio;
using SwordArt.Economy;
using SwordArt.Inventory;
using SwordArt.SaveSystem;
using SwordArt.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SwordArt.Core
{
    /// <summary>
    /// Persistent composition root. Created automatically before the first scene loads,
    /// owns the shared services (save, currency, inventory, shop, input, audio, UI) and the
    /// game session lifecycle. This is the only global singleton of the project.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>The single persistent instance.</summary>
        public static GameManager Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public ISaveSystem SaveSystem { get; private set; }
        public CurrencyService Currency { get; private set; }
        public InventoryService Inventory { get; private set; }
        public ShopSystem Shop { get; private set; }
        public GameInput Input { get; private set; }
        public SceneLoader SceneLoader { get; private set; }
        public AudioService Audio { get; private set; }
        public UIManager UI { get; private set; }

        /// <summary>Data of the running session (shared by all services).</summary>
        public SaveData Data { get; private set; }

        /// <summary>True once a gameplay session (new or continued) is running.</summary>
        public bool SessionActive { get; private set; }

        /// <summary>Spawn id requested by the last scene transition.</summary>
        public string PendingSpawnId { get; set; } = SpawnIds.Saved;

        /// <summary>World type of the currently active scene context.</summary>
        public WorldType CurrentWorld { get; private set; } = WorldType.Menu;

        /// <summary>Raised after a session was (re)started with new or loaded data.</summary>
        public event Action SessionStarted;

        /// <summary>Raised when the active scene context changes world type.</summary>
        public event Action<WorldType> WorldChanged;

        private Transform _realWorldPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Config = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            if (Config == null) Debug.LogError("[GameManager] GameConfig not found in Resources. Run SwordArt/Setup/Create Data Assets.");
            var database = Config != null ? Config.itemDatabase : null;

            SaveSystem = new JsonSaveSystem();
            Currency = new CurrencyService();
            Inventory = new InventoryService(database);
            Shop = new ShopSystem(Currency, Inventory);
            Input = new GameInput();

            EnsureEventSystem();
            Audio = gameObject.AddComponent<AudioService>();
            UI = gameObject.AddComponent<UIManager>();
            SceneLoader = gameObject.AddComponent<SceneLoader>();

            Shop.Purchased += _ => SaveGame("purchase");
            GameSettings.Apply();

            // Bind a preview of the stored data so menus can display it; the session starts later.
            BindData(SaveSystem.Load(out _) ?? CreateNewGameData());
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Input?.Dispose();
            Instance = null;
        }

        private void OnApplicationQuit()
        {
            SaveGame("quit");
        }

        /// <summary>Starts a fresh game, overwriting any existing save.</summary>
        public void NewGame()
        {
            SaveSystem.Delete();
            BindData(CreateNewGameData());
            SessionActive = true;
            SaveGame("new game");
            SessionStarted?.Invoke();
            SceneLoader.LoadScene(SceneNames.City, SpawnIds.Default, "Новая игра");
        }

        /// <summary>Loads the save and returns to the last real-world location. False if no save.</summary>
        public bool ContinueGame()
        {
            var data = SaveSystem.Load(out var status);
            if (data == null) return false;
            BindData(data);
            SessionActive = true;
            SessionStarted?.Invoke();
            if (status == LoadStatus.RecoveredFromBackup)
                UI.Notify("Сохранение повреждено — восстановлено из резервной копии", NotificationKind.Warning);
            string scene = SceneNames.IsRealWorld(data.lastRealScene) ? data.lastRealScene : SceneNames.City;
            SceneLoader.LoadScene(scene, SpawnIds.Saved, "Загрузка");
            return true;
        }

        /// <summary>
        /// Called by a gameplay <see cref="SceneContext"/>. Starts a session automatically when a
        /// gameplay scene is opened directly (e.g. from the editor).
        /// </summary>
        public void EnterScene(SceneContext context)
        {
            if (context.WorldType != WorldType.Menu && !SessionActive)
            {
                var data = SaveSystem.Load(out var status);
                if (status == LoadStatus.Corrupted)
                    UI.Notify("Файл сохранения повреждён. Начата новая игра.", NotificationKind.Warning);
                BindData(data ?? CreateNewGameData());
                SessionActive = true;
                SessionStarted?.Invoke();
            }
            if (context.WorldType == WorldType.Menu) SessionActive = false;

            CurrentWorld = context.WorldType;
            _realWorldPlayer = context.WorldType == WorldType.RealWorld ? context.Player : null;
            WorldChanged?.Invoke(CurrentWorld);
        }

        /// <summary>Captures the real-world position (if any) and writes the save file.</summary>
        public void SaveGame(string reason)
        {
            if (!SessionActive || Data == null) return;
            CaptureRealWorldPosition();
            bool ok = SaveSystem.Save(Data);
            if (ok) Debug.Log($"[GameManager] Saved ({reason}).");
        }

        /// <summary>Returns to the main menu after saving.</summary>
        public void ReturnToMainMenu()
        {
            SaveGame("main menu");
            SceneLoader.LoadScene(SceneNames.MainMenu, SpawnIds.Default, "Главное меню");
        }

        /// <summary>Saves and quits the application (stops play mode in the editor).</summary>
        public void QuitGame()
        {
            SaveGame("quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CaptureRealWorldPosition()
        {
            if (_realWorldPlayer == null) return;
            string scene = SceneManager.GetActiveScene().name;
            if (!SceneNames.IsRealWorld(scene)) return;
            Data.lastRealScene = scene;
            Data.hasRealWorldPosition = true;
            Data.realWorldPosition = _realWorldPlayer.position;
            Data.realWorldYaw = _realWorldPlayer.eulerAngles.y;
        }

        private void BindData(SaveData data)
        {
            Data = data;
            Currency.Bind(data);
            Inventory.Bind(data);
            EnsureStarterItems();
        }

        private SaveData CreateNewGameData()
        {
            var data = new SaveData
            {
                credits = Config != null && Config.economy != null ? Config.economy.startingCredits : 250,
                lastRealScene = SceneNames.City
            };
            return data;
        }

        /// <summary>Guarantees the starter items (e.g. the pistol) are always owned.</summary>
        private void EnsureStarterItems()
        {
            if (Config == null || Config.economy == null) return;
            foreach (var item in Config.economy.starterItems)
                if (item != null && !Inventory.Has(item.id)) Inventory.Add(item, 1);
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.transform.SetParent(transform);
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
