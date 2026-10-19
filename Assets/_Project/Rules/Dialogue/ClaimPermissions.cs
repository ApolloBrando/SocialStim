using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>One claim/report permission policy shared by player and NPC action generation and validation.</summary>
    public static class ClaimPermissions
    {
        /// <summary>A non-bluffer can only claim an action role they actually hold.</summary>
        public static bool CanClaim(RoleSpec own, RoleSpec claimed) => own != null && claimed != null
            && (own.CanBluff || own.CanClaimTruthfully && own.Id == claimed.Id);
        /// <summary>Enumerates exactly the claims the actor is allowed to make.</summary>
        public static IReadOnlyList<RoleSpec> Available(RoleSpec own, IEnumerable<RoleSpec> catalog)
            => catalog.Where(r => CanClaim(own, r)).OrderBy(r => r.Name, System.StringComparer.Ordinal).ToList().AsReadOnly();
        /// <summary>Actual delivered results may be shared; fabricated results require bluff permission.</summary>
        public static bool CanReport(RoleSpec own, bool matchesDeliveredResult) => own != null
            && (own.CanBluff || own.CanClaimTruthfully && matchesDeliveredResult);
    }
}
