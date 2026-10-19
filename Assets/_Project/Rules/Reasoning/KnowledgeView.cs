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
            int day, IEnumerable<KnownCharacter> characters, IEnumerable<NightReport> privateResults)
        {
            Self = self; OwnRole = ownRole; Personality = personality; Charges = charges; Day = day;
            Characters = characters.OrderBy(c => c.Id.Value).ToList().AsReadOnly();
            PrivateResults = privateResults.Where(r => r.Recipient == self).ToList().AsReadOnly();
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
    }
}
