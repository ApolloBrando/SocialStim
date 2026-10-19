using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Selects one living eligible replacement after simultaneous cleanup.</summary>
    public static class EvilSuccession
    {
        /// <summary>No promotion is needed while an Evil attack carrier lives; stable ID breaks ties.</summary>
        public static CharacterId? Successor(IReadOnlyList<NightActor> living, bool enabled)
        {
            if (!enabled || living.Any(a => a.Role.Faction == Faction.Antagonist && a.Role.Ability == AbilityKind.Attack)) return null;
            return living.Where(a => a.Role.Faction == Faction.Antagonist && a.Role.Flags.HasFlag(RoleFlags.Promotable))
                .OrderBy(a => a.Id.Value).Select(a => (CharacterId?)a.Id).FirstOrDefault();
        }
    }
}
