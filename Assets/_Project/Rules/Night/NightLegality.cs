using System.Linq;
using System.Collections.Generic;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Shared target/resource validation for player and NPC night commands.</summary>
    public static class NightLegality
    {
        /// <summary>Checks a selection against the living night snapshot.</summary>
        public static bool IsLegal(NightActor actor, NightOrder order, IReadOnlyList<NightActor> actors)
        {
            if (actor == null || order == null || order.Actor != actor.Id) return false;
            if (!order.Target.HasValue) return true;
            if (actor.GuiltDue || actor.Charges == 0 || actor.Role.Ability == AbilityKind.None) return false;
            NightActor target = actors.FirstOrDefault(a => a.Id == order.Target.Value);
            if (target == null) return false;
            if (actor.Role.Ability == AbilityKind.SelfProtect) return target.Id == actor.Id;
            if (target.Id == actor.Id) return false;
            return actor.Role.Ability != AbilityKind.Attack || actor.Role.Faction != Faction.Antagonist
                || target.Role.Faction != Faction.Antagonist;
        }
    }
}
