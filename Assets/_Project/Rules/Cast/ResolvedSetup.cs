using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Detached assignment produced by one validated setup transaction.</summary>
    public sealed class ResolvedSetup
    {
        internal ResolvedSetup(IEnumerable<RoleSpec> roles, RoleId? playerRole)
        { Roles = roles.ToList().AsReadOnly(); PlayerRole = playerRole; }
        public IReadOnlyList<RoleSpec> Roles { get; }
        public RoleId? PlayerRole { get; }
        public int Town => Roles.Count(r => r.Faction == Faction.Town);
        public int Evil => Roles.Count(r => r.Faction == Faction.Antagonist);
        public int Neutral => Roles.Count(r => r.Faction == Faction.Neutral);
    }
}
