using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Shared scoped claim choices; a claim conflict is handled by the immediate response window.</summary>
    public static class ClaimPolicy
    {
        /// <summary>Chooses an unclaimed legal role without access to hidden assignments.</summary>
        public static RoleSpec Proactive(KnowledgeView knowledge)
        {
            if (knowledge.Characters.First(c => c.Id == knowledge.Self).ClaimedRole.HasValue) return null;
            return knowledge.RoleCatalog.Where(role => knowledge.OwnRole.CanClaim(role)
                    && !knowledge.Characters.Any(c => c.Alive && c.ClaimedRole == role.Id))
                .OrderByDescending(role => role.Ability == AbilityKind.Investigate)
                .ThenBy(role => role.Id.Value, System.StringComparer.Ordinal).FirstOrDefault();
        }
    }
}
