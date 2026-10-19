using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Settings
{
    /// <summary>Versioned preferences with per-field recovery and migration; never serializes a run.</summary>
    public static class PreferencesCodec
    {
        public const int Version = 2;
        private static DataContractJsonSerializer Serializer() => new DataContractJsonSerializer(typeof(PreferencesRecord),
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        public static string Write(AppPreferences preferences)
        {
            var record = new PreferencesRecord {
                Version = Version,
                Display = Enum.GetValues(typeof(DisplaySetting)).Cast<DisplaySetting>().ToDictionary(k => k.ToString(), k => preferences.Display.Get(k)),
                Game = Enum.GetValues(typeof(GameSetting)).Cast<GameSetting>().ToDictionary(k => k.ToString(), k => preferences.Game.Get(k)),
                PlayerRole = preferences.Game.PlayerRole, PlayerFaction = preferences.Game.PlayerFaction.HasValue ? (int?)preferences.Game.PlayerFaction.Value : null,
                Seed = preferences.Game.Seed, Slots = preferences.Game.Slots.ToArray(),
                Roles = preferences.Game.Limits.Select(r => new RoleLimitRecord { Role = r.Role.Value, Minimum = r.Minimum, Maximum = r.Maximum }).ToArray()
            };
            using (var stream = new MemoryStream())
            { Serializer().WriteObject(stream, record); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public static AppPreferences Read(string json, int capacity, out string warning)
        {
            warning = null;
            if (string.IsNullOrWhiteSpace(json)) return AppPreferences.Defaults(capacity);
            try
            {
                PreferencesRecord record;
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) record = (PreferencesRecord)Serializer().ReadObject(stream);
                if (record == null || record.Version < 0 || record.Version > Version)
                { warning = "Unsupported settings version."; return AppPreferences.Defaults(capacity); }
                var display = new Dictionary<DisplaySetting, float>(); var game = new Dictionary<GameSetting, int>();
                if (record.Display != null)
                    foreach (var pair in record.Display)
                        if (Enum.TryParse(pair.Key, true, out DisplaySetting key) && Enum.IsDefined(typeof(DisplaySetting), key)
                            && string.Equals(Enum.GetName(typeof(DisplaySetting), key), pair.Key, StringComparison.OrdinalIgnoreCase)) display[key] = pair.Value;
                if (record.Game != null)
                    foreach (var pair in record.Game)
                        if (Enum.TryParse(pair.Key, true, out GameSetting key) && Enum.IsDefined(typeof(GameSetting), key)
                            && string.Equals(Enum.GetName(typeof(GameSetting), key), pair.Key, StringComparison.OrdinalIgnoreCase)) game[key] = pair.Value;
                if (record.Version < 2)
                {
                    if (record.TextSpeed.HasValue) display[DisplaySetting.Speed] = record.TextSpeed.Value == 28 ? 84 : record.TextSpeed.Value;
                    if (record.Intensity.HasValue) display[DisplaySetting.Intensity] = record.Intensity.Value;
                    if (record.ReducedMotion.HasValue) display[DisplaySetting.ReducedMotion] = record.ReducedMotion.Value ? 1 : 0;
                    if (record.Flashes.HasValue) display[DisplaySetting.Flashes] = record.Flashes.Value ? 1 : 0;
                    if (record.Instant.HasValue) display[DisplaySetting.Instant] = record.Instant.Value ? 1 : 0;
                }
                var limits = new List<RoleCountRule>();
                foreach (var role in record.Roles ?? Array.Empty<RoleLimitRecord>())
                {
                    if (role == null || string.IsNullOrWhiteSpace(role.Role) || limits.Any(r => r.Role.Value == role.Role)) continue;
                    int max = Math.Max(0, Math.Min(capacity, role.Maximum)), min = Math.Max(0, Math.Min(max, role.Minimum));
                    limits.Add(new RoleCountRule(new RoleId(role.Role), min, max));
                }
                Faction? faction = record.PlayerFaction.HasValue && Enum.IsDefined(typeof(Faction), record.PlayerFaction.Value)
                    ? (Faction?)record.PlayerFaction.Value : null;
                return new AppPreferences(new DisplayPreferences(display),
                    new GamePreferences(capacity, game, record.PlayerRole, faction, record.Seed, limits, record.Slots));
            }
            catch (Exception exception) when (exception is SerializationException || exception is ArgumentException
                || exception is System.Xml.XmlException || exception is FormatException)
            { warning = "Settings could not be read; defaults loaded."; return AppPreferences.Defaults(capacity); }
        }
    }
}
