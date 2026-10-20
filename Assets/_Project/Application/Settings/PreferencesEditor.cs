using System;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Application.Settings
{
    /// <summary>Typed preference edits and per-setting resets without mutating a current run.</summary>
    public static class PreferencesEditor
    {
        /// <summary>Returns a numeric/toggle value for a supported control.</summary>
        public static float Value(GamePreferences p, PreferenceKey key)
        {
            switch (key)
            {
                case PreferenceKey.Effects: return p.Effects.Enabled ? 1 : 0;
                case PreferenceKey.ReducedMotion: return p.Effects.ReducedMotion ? 1 : 0;
                case PreferenceKey.Flashes: return p.Effects.Flashes ? 1 : 0;
                case PreferenceKey.InstantText: return p.Effects.InstantText ? 1 : 0;
                case PreferenceKey.Intensity: return p.Effects.Intensity;
                case PreferenceKey.Speed: return p.Effects.ElementsPerSecond;
                case PreferenceKey.TextSize: return p.TextSize;
                case PreferenceKey.UiScale: return p.UiScale;
                case PreferenceKey.QuickMode: return p.QuickMode ? 1 : 0;
                case PreferenceKey.AutoAdvance: return p.AutoAdvance ? 1 : 0;
                case PreferenceKey.AdvanceDelay: return p.AdvanceDelay;
                case PreferenceKey.Colorblind: return p.Colorblind ? 1 : 0;
                case PreferenceKey.PlayerCount: return p.Game.PlayerCount;
                case PreferenceKey.Rounds: return p.Game.Rounds;
                case PreferenceKey.Windows: return p.Game.Windows;
                case PreferenceKey.Mode: return (int)p.Game.Mode;
                case PreferenceKey.Abstain: return p.Game.Abstain ? 1 : 0;
                case PreferenceKey.Revote: return p.Game.Revote ? 1 : 0;
                case PreferenceKey.Reveal: return p.Game.Reveal ? 1 : 0;
                case PreferenceKey.LastWords: return p.Game.LastWords ? 1 : 0;
                case PreferenceKey.Trials: return p.Game.Trials;
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }
        /// <summary>Changes one value, using the same field-level validation as loading a settings file.</summary>
        public static GamePreferences Apply(GamePreferences p, PreferenceKey key, float value)
        {
            if (!Enum.IsDefined(typeof(PreferenceKey), key) || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            var effects = new EffectsSettings(key == PreferenceKey.Effects ? value >= 0.5f : p.Effects.Enabled, key == PreferenceKey.ReducedMotion ? value >= 0.5f : p.Effects.ReducedMotion, key == PreferenceKey.Flashes ? value >= 0.5f : p.Effects.Flashes, key == PreferenceKey.Intensity ? value : p.Effects.Intensity, key == PreferenceKey.InstantText ? value >= 0.5f : p.Effects.InstantText, key == PreferenceKey.Speed ? value : p.Effects.ElementsPerSecond);
            var game = new NewRunSettings(key == PreferenceKey.PlayerCount ? (int)value : p.Game.PlayerCount, key == PreferenceKey.Rounds ? (int)value : p.Game.Rounds, key == PreferenceKey.Windows ? (int)value : p.Game.Windows, key == PreferenceKey.Mode ? (SocialStim.Rules.Phases.VotingMode)(int)value : p.Game.Mode, key == PreferenceKey.Abstain ? value >= 0.5f : p.Game.Abstain, key == PreferenceKey.Revote ? value >= 0.5f : p.Game.Revote, key == PreferenceKey.Reveal ? value >= 0.5f : p.Game.Reveal, key == PreferenceKey.LastWords ? value >= 0.5f : p.Game.LastWords, key == PreferenceKey.Trials ? (int)value : p.Game.Trials, p.Game.Seed);
            return new GamePreferences(effects, game, key == PreferenceKey.TextSize ? value : p.TextSize, key == PreferenceKey.UiScale ? value : p.UiScale, key == PreferenceKey.QuickMode ? value >= 0.5f : p.QuickMode, key == PreferenceKey.AutoAdvance ? value >= 0.5f : p.AutoAdvance, key == PreferenceKey.AdvanceDelay ? value : p.AdvanceDelay, key == PreferenceKey.Colorblind ? value >= 0.5f : p.Colorblind);
        }
        /// <summary>Resets exactly one field while preserving all other choices.</summary>
        public static GamePreferences Reset(GamePreferences p, PreferenceKey key) => Apply(p, key, Value(new GamePreferences(), key));
        /// <summary>Sets a seed or restores fresh-seed selection.</summary>
        public static GamePreferences Seed(GamePreferences p, uint? seed) => p.WithGame(new NewRunSettings(p.Game.PlayerCount,
            p.Game.Rounds, p.Game.Windows, p.Game.Mode, p.Game.Abstain, p.Game.Revote, p.Game.Reveal, p.Game.LastWords, p.Game.Trials, seed));
    }
}
