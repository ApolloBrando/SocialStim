using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>A participant as known by one observer, with no hidden role reference.</summary>
    public sealed class KnownCharacter
    {
        /// <summary>Creates an observer-specific public character record.</summary>
        public KnownCharacter(CharacterId id, string name, bool alive, int suspicion,
            bool teammate = false, RoleId? revealedRole = null, Faction? revealedFaction = null, RoleId? claim = null,
            int trust = 50, FactionEstimate factionEstimate = null, IEnumerable<RoleEstimate> roleEstimates = null)
        { Id = id; Name = name; Alive = alive; Suspicion = suspicion; IsTeammate = teammate;
            RevealedRole = revealedRole; RevealedFaction = revealedFaction; ClaimedRole = claim;
            Trust = trust; FactionEstimate = factionEstimate;
            RoleEstimates = (roleEstimates ?? Enumerable.Empty<RoleEstimate>()).ToList().AsReadOnly(); }
        /// <summary>Public identity.</summary>
        public CharacterId Id { get; }
        /// <summary>Public name.</summary>
        public string Name { get; }
        /// <summary>Public survival status.</summary>
        public bool Alive { get; }
        /// <summary>This observer's bounded suspicion score, not a role probability.</summary>
        public int Suspicion { get; }
        /// <summary>True only for explicitly granted antagonist teammate knowledge.</summary>
        public bool IsTeammate { get; }
        /// <summary>Role disclosed by a public reveal, if any.</summary>
        public RoleId? RevealedRole { get; }
        /// <summary>Publicly revealed faction, if any.</summary>
        public Faction? RevealedFaction { get; }
        /// <summary>Most recent public claim; may be false.</summary>
        public RoleId? ClaimedRole { get; }
        /// <summary>Observer-local credibility score, not another NPC's private opinion.</summary>
        public int Trust { get; }
        /// <summary>Normalized model estimates, or null for legacy test contexts.</summary>
        public FactionEstimate FactionEstimate { get; }
        /// <summary>Observer-local candidate roles, never an authoritative assignment.</summary>
        public IReadOnlyList<RoleEstimate> RoleEstimates { get; }
    }
}
