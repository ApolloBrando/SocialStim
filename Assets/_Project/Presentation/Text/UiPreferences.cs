using System;
using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Text
{
    /// <summary>Only presentation preferences persist. No run, role, seed, or command state is stored.</summary>
    internal static class UiPreferences
    {
        private const string Prefix = "SocialStim.UI.";
        internal static EffectsSettings Load()
        {
            if (PlayerPrefs.GetInt(Prefix + "version", 0) != 1) return new EffectsSettings();
            try
            {
                return new EffectsSettings(Flag("enabled", true), Flag("reduced", true), Flag("flashes", false),
                    PlayerPrefs.GetFloat(Prefix + "intensity", 0.5f), Flag("instant", false),
                    LoadSpeed());
            }
            catch (ArgumentException) { return new EffectsSettings(); }
        }
        private static float LoadSpeed()
        {
            float value = PlayerPrefs.GetFloat(Prefix + "speed", EffectsSettings.NormalTextSpeed);
            return float.IsNaN(value) || float.IsInfinity(value) ? EffectsSettings.NormalTextSpeed
                : Mathf.Clamp(value, 0, EffectsSettings.SpeedForPercent(200));
        }
        private static bool Flag(string name, bool fallback)
        {
            int value = PlayerPrefs.GetInt(Prefix + name, fallback ? 1 : 0);
            return value == 0 ? false : value == 1 ? true : fallback;
        }
        internal static void Store(EffectsSettings settings)
        {
            PlayerPrefs.SetInt(Prefix + "version", 1);
            PlayerPrefs.SetInt(Prefix + "enabled", settings.Enabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "reduced", settings.ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "flashes", settings.Flashes ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "instant", settings.InstantText ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "intensity", settings.Intensity);
            PlayerPrefs.SetFloat(Prefix + "speed", settings.ElementsPerSecond);
        }
    }
}
