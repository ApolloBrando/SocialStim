using System.IO;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Settings;
using UnityEngine;

namespace SocialStim.Presentation.Text
{
    /// <summary>Unity path/legacy adapter; the portable store contains no current run state.</summary>
    internal static class UiPreferences
    {
        private const string Prefix = "SocialStim.UI.";
        private static SettingsStore StoreFile => new SettingsStore(Path.Combine(UnityEngine.Application.persistentDataPath, "settings.json"));
        internal static GamePreferences Load()
        {
            var store = StoreFile;
            if (store.Exists)
            {
                var loaded = store.Load(out string warning);
                if (warning != null) Debug.LogWarning(warning);
                return loaded;
            }
            if (PlayerPrefs.GetInt(Prefix + "version", 0) != 1) return new GamePreferences();
            // Clamp each old numeric field independently; one corrupt value must not reset all preferences.
            float intensity = Safe(PlayerPrefs.GetFloat(Prefix + "intensity", 0.5f), 0, 1, 0.5f);
            float speed = Safe(PlayerPrefs.GetFloat(Prefix + "speed", 84), 1, 240, 84);
            var p = new GamePreferences(new EffectsSettings(Flag("enabled", true), Flag("reduced", true), Flag("flashes", false),
                intensity, Flag("instant", false), speed));
            try { store.Save(p); }
            catch (System.Exception e) when (e is IOException || e is System.UnauthorizedAccessException)
            { Debug.LogWarning("Legacy preferences loaded but migration could not be saved: " + e.Message); }
            return p;
        }
        internal static void Store(GamePreferences preferences) => StoreFile.Save(preferences);
        private static bool Flag(string name, bool fallback)
        { int v = PlayerPrefs.GetInt(Prefix + name, fallback ? 1 : 0); return v == 0 ? false : v == 1 ? true : fallback; }
        private static float Safe(float value, float low, float high, float fallback) => float.IsNaN(value) || float.IsInfinity(value)
            ? fallback : Mathf.Clamp(value, low, high);
    }
}
