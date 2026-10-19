using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Validated immutable setup draft; counts are total participants including the player.</summary>
    public sealed class SetupConfiguration
    {
        public SetupConfiguration(int participants = 9, SetupPreset preset = SetupPreset.Classic, int evil = -1, int neutral = -1,
            RoleId? playerRole = null, Faction? playerFaction = null, IEnumerable<RoleCountRule> roleCounts = null,
            IEnumerable<RoleSlot> slots = null, SetupVisibility visibility = SetupVisibility.FactionCounts)
        {
            if (participants < 5 || evil < -1 || neutral < -1) throw new ArgumentException("At least five participants; counts must be Auto or nonnegative.");
            if (!Enum.IsDefined(typeof(SetupPreset), preset) || !Enum.IsDefined(typeof(SetupVisibility), visibility)
                || playerFaction.HasValue && !Enum.IsDefined(typeof(Faction), playerFaction.Value)) throw new ArgumentException("Unknown setup setting.");
            Participants = participants; Preset = preset; Evil = evil; Neutral = neutral; PlayerRole = playerRole;
            PlayerFaction = playerFaction; Visibility = visibility;
            RoleCounts = (roleCounts ?? Enumerable.Empty<RoleCountRule>()).ToList().AsReadOnly();
            Slots = (slots ?? Enumerable.Empty<RoleSlot>()).ToList().AsReadOnly();
        }
        public int Participants { get; }
        public SetupPreset Preset { get; }
        public int Evil { get; }
        public int Neutral { get; }
        public RoleId? PlayerRole { get; }
        public Faction? PlayerFaction { get; }
        public SetupVisibility Visibility { get; }
        public IReadOnlyList<RoleCountRule> RoleCounts { get; }
        public IReadOnlyList<RoleSlot> Slots { get; }
    }
}
