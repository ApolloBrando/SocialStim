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
                context.Reports.Add(new NightReport(order.Target.Value, ReportKind.Targeted));
                if (role.Attack > context.Defense[order.Target.Value]) context.Deaths.Add(order.Target.Value);
                else context.Reports.Add(new NightReport(order.Actor, ReportKind.AttackFailed, order.Target));
            }
        }
    }
}
