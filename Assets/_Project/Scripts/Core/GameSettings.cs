using System;
using UnityEngine;

namespace SwordArt.Core
{
    /// <summary>
    /// Player preferences (volume, mouse sensitivity). Stored in PlayerPrefs, separate from save games.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "settings.volume";
        private const string SensitivityKey = "settings.sensitivity";
        private const string InvertYKey = "settings.invertY";

        /// <summary>Raised after any setting changes.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        /// <summary>Master volume in [0, 1].</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); Apply(); }
        }

        /// <summary>Mouse sensitivity multiplier in [0.1, 3].</summary>
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.1f, 3f)); Apply(); }
        }

        /// <summary>Inverts vertical mouse look.</summary>
        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            set { PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0); Apply(); }
        }

        /// <summary>Applies settings to engine systems and notifies listeners.</summary>
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
