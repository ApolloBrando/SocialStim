using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>The complete restricted input to an NPC strategy.</summary>
    public sealed class KnowledgeView
    {
        /// <summary>Copies allowed knowledge; true roles of other characters are absent.</summary>
        public KnowledgeView(CharacterId self, RoleSpec ownRole, Personality personality, int charges,
            int day, IEnumerable<KnownCharacter> characters, IEnumerable<NightReport> privateResults,
            IEnumerable<EvidenceRecord> evidence = null, int selfExposure = 0, IEnumerable<RoleSpec> roleCatalog = null, TeamNightPlan teamPlan = null,
            IEnumerable<CharacterId> nominationCandidates = null, NightActionClaim lastNightAction = null, bool canGiveReport = false)
        {
            Self = self; OwnRole = ownRole; Personality = personality; Charges = charges; Day = day;
            Characters = characters.OrderBy(c => c.Id.Value).ToList().AsReadOnly();
            PrivateResults = privateResults.Where(r => r.Recipient == self).ToList().AsReadOnly();
            Evidence = (evidence ?? Enumerable.Empty<EvidenceRecord>()).Where(e => e.Kind != EvidenceKind.PrivateInvestigation || e.Actor == self).ToList().AsReadOnly();
            SelfExposure = selfExposure;
            RoleCatalog = (roleCatalog ?? new[] { ownRole }).OrderBy(r => r.Id.Value, System.StringComparer.Ordinal).ToList().AsReadOnly();
            TeamPlan = ownRole.Faction == Faction.Antagonist ? teamPlan : null;
            NominationCandidates = nominationCandidates?.Distinct().ToList().AsReadOnly();
            LastNightAction = lastNightAction; CanGiveReport = canGiveReport;
        }
        /// <summary>Observer identity.</summary>
        public CharacterId Self { get; }
        /// <summary>Only the observer's true role definition.</summary>
        public RoleSpec OwnRole { get; }
        /// <summary>Observer's behavioral tendencies.</summary>
        public Personality Personality { get; }
        /// <summary>Observer's remaining ability resources.</summary>
        public int Charges { get; }
        /// <summary>Public day counter.</summary>
        public int Day { get; }
        /// <summary>Permitted observations about the cast.</summary>
        public IReadOnlyList<KnownCharacter> Characters { get; }
        /// <summary>Results legitimately delivered to this observer.</summary>
        public IReadOnlyList<NightReport> PrivateResults { get; }
        /// <summary>Observer's evidence history, never foreign private observations.</summary>
        public IReadOnlyList<EvidenceRecord> Evidence { get; }
        /// <summary>Estimated exposure from public reactions; other minds are not read.</summary>
        public int SelfExposure { get; }
        /// <summary>Public definition catalog, not the cast's assigned roles.</summary>
        public IReadOnlyList<RoleSpec> RoleCatalog { get; }
        /// <summary>Previous authorized faction plan; always null for other factions.</summary>
        public TeamNightPlan TeamPlan { get; }
        /// <summary>Public legal targets in a restricted revote; null means all living opponents.</summary>
        public IReadOnlyList<CharacterId> NominationCandidates { get; }
        public NightActionClaim LastNightAction { get; }
        public bool CanGiveReport { get; }
    }
}
