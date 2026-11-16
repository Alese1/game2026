using System;
using System.IO;
using UnityEngine;

namespace SwordArt.SaveSystem
{
    /// <summary>Outcome of a load attempt.</summary>
    public enum LoadStatus
    {
        Loaded,
        NoSave,
        RecoveredFromBackup,
        Corrupted
    }

    /// <summary>Persistence abstraction for <see cref="SaveData"/>.</summary>
    public interface ISaveSystem
    {
        /// <summary>True when a readable save file exists.</summary>
        bool HasSave { get; }

        /// <summary>Loads the save. Returns null when there is no usable save.</summary>
        SaveData Load(out LoadStatus status);

        /// <summary>Writes <paramref name="data"/> to disk. Returns false on IO failure.</summary>
        bool Save(SaveData data);

        /// <summary>Deletes the save and its backup.</summary>
        void Delete();
    }

    /// <summary>
    /// JSON save files in <see cref="Application.persistentDataPath"/>, written atomically
    /// with a backup copy so a crash during save cannot destroy progress.
    /// </summary>
    public sealed class JsonSaveSystem : ISaveSystem
    {
        private readonly string _path;
        private readonly string _backupPath;
        private readonly string _tempPath;

        /// <param name="fileName">Save file name inside persistentDataPath.</param>
        public JsonSaveSystem(string fileName = "savegame.json")
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
            _backupPath = _path + ".bak";
            _tempPath = _path + ".tmp";
        }

        /// <summary>Absolute path of the main save file.</summary>
        public string FilePath => _path;

        public bool HasSave => TryRead(_path, out _) || TryRead(_backupPath, out _);

        public SaveData Load(out LoadStatus status)
        {
            if (!File.Exists(_path) && !File.Exists(_backupPath))
            {
                status = LoadStatus.NoSave;
                return null;
            }

            if (TryRead(_path, out var data))
            {
                status = LoadStatus.Loaded;
                return data;
            }

            Debug.LogWarning($"[SaveSystem] Save file is corrupted: {_path}");
            QuarantineCorruptFile();

            if (TryRead(_backupPath, out data))
            {
                status = LoadStatus.RecoveredFromBackup;
                Save(data);
                return data;
            }

            status = LoadStatus.Corrupted;
            return null;
        }

        public bool Save(SaveData data)
        {
            if (data == null) return false;
            try
            {
                data.version = SaveData.CurrentVersion;
                data.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(data, true);
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_tempPath, json);
                if (File.Exists(_path)) File.Copy(_path, _backupPath, true);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(_tempPath, _path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
                return false;
            }
        }

        public void Delete()
        {
            foreach (var p in new[] { _path, _backupPath, _tempPath })
            {
                try { if (File.Exists(p)) File.Delete(p); }
                catch (Exception e) { Debug.LogWarning($"[SaveSystem] Could not delete {p}: {e.Message}"); }
            }
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<SaveData>(json);
                return Validate(data);
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        /// <summary>Sanity-checks and repairs loaded data. Returns false if unusable.</summary>
        private static bool Validate(SaveData data)
        {
            if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion) return false;
            if (data.credits < 0) data.credits = 0;
            data.inventory ??= new System.Collections.Generic.List<InventoryEntry>();
            data.inventory.RemoveAll(e => e == null || string.IsNullOrEmpty(e.itemId) || e.count <= 0);
            data.progress ??= new ProgressData();
            if (float.IsNaN(data.realWorldPosition.x) || float.IsNaN(data.realWorldPosition.y) || float.IsNaN(data.realWorldPosition.z))
                data.hasRealWorldPosition = false;
            return true;
        }

        private void QuarantineCorruptFile()
        {
            try
            {
                if (!File.Exists(_path)) return;
                string corruptPath = _path + ".corrupt";
                if (File.Exists(corruptPath)) File.Delete(corruptPath);
                File.Move(_path, corruptPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not quarantine corrupt save: {e.Message}");
            }
        }
    }
}
