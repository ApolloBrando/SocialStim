using System;
using System.Collections.Generic;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Application.Settings
{
    /// <summary>Immutable validated display preferences. Boolean settings use exact zero/one in the versioned file.</summary>
    public sealed class DisplayPreferences
    {
        private readonly Dictionary<DisplaySetting, float> _values;
        public DisplayPreferences(IReadOnlyDictionary<DisplaySetting, float> values = null)
        {
            _values = new Dictionary<DisplaySetting, float>();
            foreach (DisplaySetting key in Enum.GetValues(typeof(DisplaySetting)))
            {
                float value = values != null && values.TryGetValue(key, out float stored) ? stored : Default(key);
                _values[key] = Normalize(key, value);
            }
        }
        public float Get(DisplaySetting key) => _values[key];
        public bool On(DisplaySetting key) => Get(key) > 0.5f;
        public DisplayPreferences With(DisplaySetting key, float value)
        { var copy = new Dictionary<DisplaySetting, float>(_values); copy[key] = value; return new DisplayPreferences(copy); }
        public DisplayPreferences Reset(DisplaySetting key) => With(key, Default(key));
        public EffectsSettings Effects() => new EffectsSettings(On(DisplaySetting.Effects), On(DisplaySetting.ReducedMotion),
            On(DisplaySetting.Flashes), Get(DisplaySetting.Intensity), On(DisplaySetting.Instant), Get(DisplaySetting.Speed));
        public static float Default(DisplaySetting key)
        {
            switch (key)
            {
                case DisplaySetting.Speed: return 84;
                case DisplaySetting.AutoDelay: return 0.35f;
                case DisplaySetting.FontSize: return 25;
                case DisplaySetting.UiScale: case DisplaySetting.Effects: case DisplaySetting.ReducedMotion:
                case DisplaySetting.Colorblind: return 1;
                case DisplaySetting.Intensity: return 0.5f;
                default: return 0;
            }
        }
        private static float Normalize(DisplaySetting key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return Default(key);
            switch (key)
            {
                case DisplaySetting.Speed: return Clamp(value, 1, 240);
                case DisplaySetting.AutoDelay: return Clamp(value, 0.1f, 3);
                case DisplaySetting.FontSize: return Clamp(value, 16, 32);
                case DisplaySetting.UiScale: return Clamp(value, 0.75f, 1.25f);
                case DisplaySetting.Intensity: return Clamp(value, 0, 1);
                default: return value >= 0.5f ? 1 : 0;
            }
        }
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
