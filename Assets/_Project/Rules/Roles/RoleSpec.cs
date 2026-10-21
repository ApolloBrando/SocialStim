using System;

namespace SocialStim.Rules.Roles
{
    /// <summary>Immutable, validated rule definition detached from Unity assets.</summary>
    public sealed class RoleSpec
    {
        /// <summary>Creates a role. A charge count of -1 means unlimited uses.</summary>
        public RoleSpec(RoleId id, string name, Faction faction, RoleCategory category,
            AbilityKind ability, int attack, int defense, int protection, int charges,
            Faction appearance, bool unique = false, bool? canClaimTruthfully = null, bool? canBluff = null,
            bool nightImmune = false, bool stealsParityWin = false, bool diesAfterTownKill = false, bool executionRevenge = false)
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
            Appearance = appearance; Unique = unique;
            CanClaimTruthfully = canClaimTruthfully ?? (faction == Faction.Town && ability != AbilityKind.None);
            CanBluff = canBluff ?? faction != Faction.Town;
            NightImmune = nightImmune; StealsParityWin = stealsParityWin;
            DiesAfterTownKill = diesAfterTownKill; ExecutionRevenge = executionRevenge;
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
        /// <summary>Whether this role may disclose its own Town special role.</summary>
        public bool CanClaimTruthfully { get; }
        /// <summary>Whether this role may impersonate a Town special role.</summary>
        public bool CanBluff { get; }
        /// <summary>Passive immunity to every night attack, independent of strength.</summary>
        public bool NightImmune { get; }
        /// <summary>Whether this surviving role replaces an antagonist parity victory.</summary>
        public bool StealsParityWin { get; }
        /// <summary>Whether a successful attack on Town schedules unavoidable guilt for the following night.</summary>
        public bool DiesAfterTownKill { get; }
        /// <summary>Whether day execution grants revenge against a living Town participant.</summary>
        public bool ExecutionRevenge { get; }
        /// <summary>Shared player/NPC claim permission; ordinary citizens are never claimable.</summary>
        public bool CanClaim(RoleSpec asserted) => asserted != null && asserted.Faction == Faction.Town
            && asserted.Ability != AbilityKind.None && (asserted.Id == Id ? CanClaimTruthfully : CanBluff && Faction != Faction.Town);
    }

    /// <summary>Initial ability implementations, reusable by content-defined roles.</summary>
    public enum AbilityKind { Investigate, Protect, Block, Attack, SelfProtect, None, Forge, Watch }

    /// <summary>Major alignment; neutral personal goals are evaluated separately.</summary>
    public enum Faction { Town, Antagonist, Neutral }

    /// <summary>Setup-generation buckets for the initial catalog.</summary>
    public enum RoleCategory { TownInvestigative, TownProtective, TownSupport, AntagonistKilling, NeutralBenign, TownKilling, TownBenign, AntagonistDeception, AntagonistSupport, NeutralEvil, NeutralKilling }
}
