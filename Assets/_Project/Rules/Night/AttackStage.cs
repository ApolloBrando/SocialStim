using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Attacks execute simultaneously; equality with defense does not kill.</summary>
    public sealed class AttackStage : INightStage
    {
        /// <inheritdoc />
        public int Priority => 40;
        /// <inheritdoc />
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order)) continue;
                RoleSpec role = context.Actor(order.Actor).Role;
                if (role.Ability != AbilityKind.Attack) continue;
                // Several attacks are one survivor-visible event, not separate clues about attacker count.
                if (!context.Reports.Any(r => r.Recipient == order.Target.Value && r.Kind == ReportKind.Targeted))
                    context.Reports.Add(new NightReport(order.Target.Value, ReportKind.Targeted));
                if (!context.Actor(order.Target.Value).Role.NightImmune && role.Attack > context.Defense[order.Target.Value])
                {
                    context.Deaths.Add(order.Target.Value);
                    if (role.DiesAfterTownKill && context.Actor(order.Target.Value).Role.Faction == Faction.Town)
                        context.TownKillers.Add(order.Actor);
                }
                else context.Reports.Add(new NightReport(order.Actor, ReportKind.AttackFailed, order.Target));
            }
        }
    }
}
