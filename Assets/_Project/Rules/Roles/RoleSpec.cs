using System;

namespace SocialStim.Rules.Roles
{
    /// <summary>Immutable, validated rule definition detached from Unity assets.</summary>
    public sealed class RoleSpec
    {
        /// <summary>Creates a role. A charge count of -1 means unlimited uses.</summary>
        public RoleSpec(RoleId id, string name, Faction faction, RoleCategory category,
            AbilityKind ability, int attack, int defense, int protection, int charges,
            Faction appearance, bool unique = false, bool? canClaimTruthfully = null, bool? canBluff = null)
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
            CanClaimTruthfully = canClaimTruthfully ?? ability != AbilityKind.None;
            CanBluff = canBluff ?? (faction == Faction.Antagonist || category == RoleCategory.NeutralEvil || category == RoleCategory.NeutralKilling);
            Appearance = appearance; Unique = unique;
        }

        /// <summary>Permission to disclose the actual role.</summary>
        public bool CanClaimTruthfully { get; }
        /// <summary>Permission to claim a different role or fabricate a result.</summary>
        public bool CanBluff { get; }
        /// <summary>Shared permission used by player menus and NPC command validation.</summary>
        public bool CanClaim(RoleId claim) => claim == Id ? CanClaimTruthfully : CanBluff;
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
    }
}
