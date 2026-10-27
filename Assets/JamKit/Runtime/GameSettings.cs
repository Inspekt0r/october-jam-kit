using System;
using UnityEngine;

namespace JamKit
{
    [Serializable]
    public sealed class SettingsData
    {
        public float master = 0.8f;
        public float music = 0.5f;
        public float effects = 0.7f;
        public float brightness = 1f;
        public bool fullscreen;
        public bool highQuality = true;

        public void Clamp()
        {
            master = Valid(master, 0.8f, 0, 1);
            music = Valid(music, 0.5f, 0, 1);
            effects = Valid(effects, 0.7f, 0, 1);
            brightness = Valid(brightness, 1, 0.35f, 1);
        }

        private static float Valid(float value, float fallback, float min, float max)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }
    }

    public static class GameSettings
    {
        public const string StorageKey = "JamKit.Settings.v1";
        public static SettingsData Current { get; private set; } = new SettingsData();
        public static event Action Changed;

        public static void Load()
        {
            Current = new SettingsData();
            try { if (PlayerPrefs.HasKey(StorageKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(StorageKey), Current); }
            catch (ArgumentException) { Current = new SettingsData(); }
            Current.Clamp();
            Apply();
        }

        public static void Save()
        {
            Current.Clamp();
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            Current = new SettingsData();
            Apply();
            Save();
        }

        public static void Apply()
        {
            Current.Clamp();
            AudioListener.volume = Current.master;
            Screen.fullScreenMode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            QualitySettings.antiAliasing = Current.highQuality ? 4 : 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Current.highQuality ? 60 : 30;
            Changed?.Invoke();
        }
    }
}
