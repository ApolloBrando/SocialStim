using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Settings
{
    /// <summary>Immutable game configuration; contains no cast, resource, relationship or ongoing-run state.</summary>
    public sealed class GamePreferences
    {
        private readonly Dictionary<GameSetting, int> _values;
        public GamePreferences(int capacity, IReadOnlyDictionary<GameSetting, int> values = null, string playerRole = null,
            Faction? playerFaction = null, string seed = "", IEnumerable<RoleCountRule> limits = null, IEnumerable<string> slots = null)
        {
            Capacity = Math.Max(5, capacity); _values = new Dictionary<GameSetting, int>();
            foreach (GameSetting key in Enum.GetValues(typeof(GameSetting)))
                _values[key] = Normalize(key, values != null && values.TryGetValue(key, out int stored) ? stored : Default(key), Capacity);
            PlayerRole = string.IsNullOrWhiteSpace(playerRole) ? null : playerRole;
            PlayerFaction = playerFaction.HasValue && Enum.IsDefined(typeof(Faction), playerFaction.Value) ? playerFaction : null;
            Seed = seed ?? "";
            Limits = (limits ?? Enumerable.Empty<RoleCountRule>()).ToList().AsReadOnly();
            Slots = (slots ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
        }
        public int Capacity { get; }
        public string PlayerRole { get; }
        public Faction? PlayerFaction { get; }
        public string Seed { get; }
        public IReadOnlyList<RoleCountRule> Limits { get; }
        public IReadOnlyList<string> Slots { get; }
        public int Get(GameSetting key) => _values[key];
        public bool On(GameSetting key) => Get(key) != 0;
        public GamePreferences With(GameSetting key, int value)
        {
            var copy = new Dictionary<GameSetting, int>(_values); copy[key] = value;
            var slots = Slots.Count > 0 && key == GameSetting.PlayerCount
                ? Slots.Take(Normalize(key, value, Capacity)).Concat(Enumerable.Repeat("Any", Math.Max(0, Normalize(key, value, Capacity) - Slots.Count)))
                : Slots;
            return new GamePreferences(Capacity, copy, PlayerRole, PlayerFaction, Seed, Limits, slots);
        }
        public GamePreferences WithPlayer(string role, Faction? faction) => new GamePreferences(Capacity, _values, role, faction, Seed, Limits, Slots);
        public GamePreferences WithSeed(string seed) => new GamePreferences(Capacity, _values, PlayerRole, PlayerFaction, seed, Limits, Slots);
        public GamePreferences WithLimit(RoleId role, int min, int max) => new GamePreferences(Capacity, _values, PlayerRole, PlayerFaction, Seed,
            Limits.Where(r => r.Role != role).Concat(new[] { new RoleCountRule(role, min, max) }), Slots);
        public GamePreferences WithSlot(int index, string code)
        {
            var slots = Enumerable.Range(0, Get(GameSetting.PlayerCount)).Select(i => i < Slots.Count ? Slots[i] : "Any").ToArray();
            if (index < 0 || index >= slots.Length) throw new ArgumentOutOfRangeException(nameof(index));
            slots[index] = code;
            return new GamePreferences(Capacity, _values, PlayerRole, PlayerFaction, Seed, Limits, slots);
        }
        public GamePreferences Reset(GameSetting key) => With(key, Default(key));
        public SetupConfiguration Setup(RoleId? roleOverride = null, Faction? factionOverride = null)
        {
            return new SetupConfiguration(Get(GameSetting.PlayerCount), (SetupPreset)Get(GameSetting.Preset),
                Get(GameSetting.EvilCount), Get(GameSetting.NeutralCount),
                roleOverride ?? (PlayerRole == null ? (RoleId?)null : new RoleId(PlayerRole)), factionOverride ?? PlayerFaction,
                Limits, Slots.Select(ParseSlot), (SetupVisibility)Get(GameSetting.Visibility));
        }
        public CouncilOptions Options(uint seed) => new CouncilOptions(seed, Get(GameSetting.Rounds), Get(GameSetting.Windows),
            On(GameSetting.Revote), On(GameSetting.Abstain), On(GameSetting.RevealRoles), On(GameSetting.LastWords),
            On(GameSetting.FreeTime), Get(GameSetting.FreeTimeActions), On(GameSetting.PromoteEvil), On(GameSetting.Backlash),
            On(GameSetting.SelfHeal), On(GameSetting.NotifyLull), Get(GameSetting.NullifierCharges), On(GameSetting.ForgerReadsClear),
            Get(GameSetting.MaximumDays), (SetupVisibility)Get(GameSetting.Visibility), (Rules.Victory.NeutralParityMode)Get(GameSetting.InfinitumParity), (AiDifficulty)Get(GameSetting.Difficulty),
            (StatsMode)Get(GameSetting.StatsMode), new CharacterStats(Get(GameSetting.Charisma), Get(GameSetting.Intuition),
                Get(GameSetting.Logic), Get(GameSetting.Charm), Get(GameSetting.Performance), Get(GameSetting.Stealth)));
        private static RoleSlot ParseSlot(string code) => code == "Any" || string.IsNullOrWhiteSpace(code) ? new RoleSlot()
            : code == "Town" ? new RoleSlot(faction: Faction.Town) : code == "Evil" ? new RoleSlot(faction: Faction.Antagonist)
            : code == "Neutral" ? new RoleSlot(faction: Faction.Neutral) : new RoleSlot(new RoleId(code));
        public static int Default(GameSetting key)
        {
            switch (key)
            {
                case GameSetting.PlayerCount: return 9;
                case GameSetting.EvilCount: case GameSetting.NeutralCount: return -1;
                case GameSetting.Rounds: return 5;
                case GameSetting.Windows: case GameSetting.Revote: case GameSetting.Abstain: case GameSetting.RevealRoles:
                case GameSetting.LastWords: case GameSetting.FreeTimeActions: case GameSetting.PromoteEvil:
                case GameSetting.Backlash: case GameSetting.SelfHeal: case GameSetting.NotifyLull:
                case GameSetting.Visibility: case GameSetting.Difficulty: return 1;
                case GameSetting.NullifierCharges: return 2;
                case GameSetting.MaximumDays: return 30;
                case GameSetting.Charisma: case GameSetting.Intuition: case GameSetting.Logic: case GameSetting.Charm:
                case GameSetting.Performance: case GameSetting.Stealth: return 5;
                default: return 0;
            }
        }
        private static int Normalize(GameSetting key, int value, int capacity)
        {
            switch (key)
            {
                case GameSetting.PlayerCount: return Clamp(value, 5, capacity);
                case GameSetting.EvilCount: case GameSetting.NeutralCount: return Clamp(value, -1, capacity - 1);
                case GameSetting.Rounds: return Clamp(value, 3, 7);
                case GameSetting.Windows: case GameSetting.FreeTimeActions: return Clamp(value, 1, 2);
                case GameSetting.NullifierCharges: return Clamp(value, 0, 10);
                case GameSetting.MaximumDays: return Clamp(value, 1, 100);
                case GameSetting.Visibility: case GameSetting.StatsMode: case GameSetting.InfinitumParity: return Clamp(value, 0, 2);
                case GameSetting.Preset: case GameSetting.Difficulty: return Clamp(value, 0, 3);
                case GameSetting.Charisma: case GameSetting.Intuition: case GameSetting.Logic: case GameSetting.Charm:
                case GameSetting.Performance: case GameSetting.Stealth: return Clamp(value, 1, 10);
                default: return value == 0 ? 0 : 1;
            }
        }
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    }
}
