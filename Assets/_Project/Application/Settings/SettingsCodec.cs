using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Settings
{
    /// <summary>Versioned JSON preferences with per-field defaults/clamping and v1 migration.</summary>
    public static class SettingsCodec
    {
        [DataContract]
        private sealed class Record
        {
            [DataMember] public int version = 2;
            [DataMember] public bool? enabled, reducedMotion, flashes, instantText, quickMode, autoAdvance, colorblind;
            [DataMember] public float? intensity, speed, textSize, uiScale, advanceDelay;
            [DataMember] public int? playerCount, rounds, windows, mode, trials;
            [DataMember] public bool? abstain, revote, reveal, lastWords;
            [DataMember] public uint? seed;
        }
        /// <summary>Serializes only preferences, never roles, events, beliefs or current phase.</summary>
        public static string Encode(GamePreferences p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var r = new Record { enabled = p.Effects.Enabled, reducedMotion = p.Effects.ReducedMotion, flashes = p.Effects.Flashes,
                instantText = p.Effects.InstantText, intensity = p.Effects.Intensity, speed = p.Effects.ElementsPerSecond,
                textSize = p.TextSize, uiScale = p.UiScale, quickMode = p.QuickMode, autoAdvance = p.AutoAdvance,
                advanceDelay = p.AdvanceDelay, colorblind = p.Colorblind, playerCount = p.Game.PlayerCount,
                rounds = p.Game.Rounds, windows = p.Game.Windows, mode = (int)p.Game.Mode, trials = p.Game.Trials,
                abstain = p.Game.Abstain, revote = p.Game.Revote, reveal = p.Game.Reveal, lastWords = p.Game.LastWords, seed = p.Game.Seed };
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(Record)).WriteObject(stream, r); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        /// <summary>Loads versions 1/2; unsupported future files fail rather than being silently downgraded.</summary>
        public static GamePreferences Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 65536) throw new ArgumentException("Settings file is empty or too large.");
            Record r;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    r = (Record)new DataContractJsonSerializer(typeof(Record)).ReadObject(stream);
            }
            catch (Exception e) when (e is SerializationException || e is System.Xml.XmlException || e is FormatException)
            { throw new ArgumentException("Settings JSON is malformed.", e); }
            if (r == null || r.version < 1 || r.version > 2) throw new ArgumentException("Unsupported settings version.");
            var effects = new EffectsSettings(r.enabled ?? true, r.reducedMotion ?? true, r.flashes ?? false,
                Clamp(r.intensity, 0, 1, 0.5f), r.instantText ?? false, Clamp(r.speed, 1, 240, 84));
            var game = new NewRunSettings(Clamp(r.playerCount, 5, 20, 9), Clamp(r.rounds, 3, 7, 5), Clamp(r.windows, 1, 2, 1),
                r.mode.HasValue && Enum.IsDefined(typeof(VotingMode), r.mode.Value) ? (VotingMode)r.mode.Value : VotingMode.Gnosia,
                r.abstain ?? true, r.revote ?? true, r.reveal ?? true, r.lastWords ?? true, Clamp(r.trials, 1, 10, 2), r.seed);
            return new GamePreferences(effects, game, Clamp(r.textSize, 16, 36, 22), Clamp(r.uiScale, 0.75f, 1.5f, 1),
                r.quickMode ?? false, r.autoAdvance ?? false, Clamp(r.advanceDelay, 0.25f, 5, 1), r.colorblind ?? false);
        }
        private static int Clamp(int? value, int low, int high, int fallback) => value.HasValue ? Math.Max(low, Math.Min(high, value.Value)) : fallback;
        private static float Clamp(float? value, float low, float high, float fallback) => !value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value)
            ? fallback : Math.Max(low, Math.Min(high, value.Value));
    }
}
