using System;

namespace SocialStim.Rules.Roles
{
    /// <summary>Immutable, validated rule definition detached from Unity assets.</summary>
    public sealed class RoleSpec
    {
        /// <summary>Creates a role. A charge count of -1 means unlimited uses.</summary>
        public RoleSpec(RoleId id, string name, Faction faction, RoleCategory category,
            AbilityKind ability, int attack, int defense, int protection, int charges,
            Faction appearance, bool unique = false, bool canClaimTruthfully = true, bool canBluff = false,
            RoleFlags flags = RoleFlags.None, RoleObjective objective = RoleObjective.Auto)
        {
            if (string.IsNullOrWhiteSpace(id.Value) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role identity and name are required.");
            if (attack < 0 || defense < 0 || protection < 0 || charges < -1)
                throw new ArgumentOutOfRangeException(nameof(attack), "Invalid role strength or resources.");
            if (!Enum.IsDefined(typeof(Faction), faction) || !Enum.IsDefined(typeof(Faction), appearance)
                || !Enum.IsDefined(typeof(RoleCategory), category) || !Enum.IsDefined(typeof(AbilityKind), ability))
                throw new ArgumentException("Unknown role enum value.");
            Faction categoryFaction = category == RoleCategory.TownInvestigative || category == RoleCategory.TownProtective
                || category == RoleCategory.TownSupport || category == RoleCategory.TownKilling || category == RoleCategory.TownBenign
                ? Faction.Town : category == RoleCategory.AntagonistKilling || category == RoleCategory.AntagonistDeception
                || category == RoleCategory.AntagonistSupport ? Faction.Antagonist : Faction.Neutral;
            if (faction != categoryFaction) throw new ArgumentException("Role faction and category must agree.");
            Id = id; Name = name; Faction = faction; Category = category; Ability = ability;
            Attack = attack; Defense = defense; Protection = protection; Charges = charges;
            if ((flags & ~(RoleFlags.NightImmune | RoleFlags.ReadsClear | RoleFlags.SelfHeal | RoleFlags.Backlash | RoleFlags.Promotable)) != 0
                || !Enum.IsDefined(typeof(RoleObjective), objective)) throw new ArgumentException("Unknown role capability/objective.");
            Appearance = appearance; Unique = unique; Flags = flags;
            CanClaimTruthfully = canClaimTruthfully && ability != AbilityKind.None; CanBluff = canBluff;
            Objective = objective != RoleObjective.Auto ? objective : faction != Faction.Neutral ? RoleObjective.Faction
                : category == RoleCategory.NeutralKilling ? RoleObjective.LastSurvivor
                : category == RoleCategory.NeutralEvil ? RoleObjective.EliminatedByVote : RoleObjective.Survive;
        }

        /// <summary>Stable role key.</summary>
        public RoleId Id { get; }
        /// <summary>Authored display name.</summary>
        public string Name { get; }
        /// <summary>True alignment, exposed only through authorized knowledge.</summary>
        public Faction Faction { get; }
        /// <summary>Role-list bucket.</summary>
        public RoleCategory Category { get; }
        /// <summary>Registered initial ability.</summary>
        public AbilityKind Ability { get; }
        /// <summary>Attack strength.</summary>
        public int Attack { get; }
        /// <summary>Innate defense.</summary>
        public int Defense { get; }
        /// <summary>Defense granted by protection.</summary>
        public int Protection { get; }
        /// <summary>Initial resources, or -1 for unlimited.</summary>
        public int Charges { get; }
        /// <summary>Alignment reported by an investigator.</summary>
        public Faction Appearance { get; }
        /// <summary>Whether a setup may contain only one copy.</summary>
        public bool Unique { get; }
        /// <summary>Whether this action role may reveal its own identity.</summary>
        public bool CanClaimTruthfully { get; }
        /// <summary>Whether it may assert other roles and fabricated results.</summary>
        public bool CanBluff { get; }
        /// <summary>Reusable passive/resource capabilities.</summary>
        public RoleFlags Flags { get; }
        /// <summary>Personal win policy.</summary>
        public RoleObjective Objective { get; }
        /// <summary>Unforged apparent readout.</summary>
        public SocialStim.Rules.Night.Readout Readout => Flags.HasFlag(RoleFlags.ReadsClear)
            ? SocialStim.Rules.Night.Readout.Clear : Objective == RoleObjective.LastSurvivor
            ? SocialStim.Rules.Night.Readout.Anomaly : Appearance == Faction.Antagonist
            ? SocialStim.Rules.Night.Readout.Corrupt : SocialStim.Rules.Night.Readout.Clear;
    }
}
