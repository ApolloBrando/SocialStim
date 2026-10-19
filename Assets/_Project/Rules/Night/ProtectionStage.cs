using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Protection establishes defense before attacks; multiple protectors take the maximum.</summary>
    public sealed class ProtectionStage : INightStage
    {
        /// <inheritdoc />
        public int Priority => 30;
        /// <inheritdoc />
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order)) continue;
                RoleSpec role = context.Actor(order.Actor).Role;
                if (role.Ability != AbilityKind.Protect && role.Ability != AbilityKind.SelfProtect) continue;
                if (context.Policy.Mission && role.Ability == AbilityKind.Protect)
                {
                    context.Heals.TryGetValue(order.Target.Value, out int tokens);
                    context.Heals[order.Target.Value] = tokens + 1;
                    context.Patients.Add(order.Actor, order.Target.Value);
                    if (order.Actor == order.Target.Value) context.SelfHealsSpent.Add(order.Actor);
                    continue;
                }
                context.Defense[order.Target.Value] = Math.Max(context.Defense[order.Target.Value], role.Protection);
                context.Reports.Add(new NightReport(order.Actor, ReportKind.ProtectionApplied, order.Target));
            }
        }
    }
}
