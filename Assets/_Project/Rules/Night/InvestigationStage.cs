using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Reports appearance, never a true hidden role; dying investigators still act.</summary>
    public sealed class InvestigationStage : INightStage
    {
        /// <inheritdoc />
        public int Priority => 50;
        /// <inheritdoc />
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order) || context.Actor(order.Actor).Role.Ability != AbilityKind.Investigate) continue;
                context.Reports.Add(new NightReport(order.Actor, ReportKind.Investigation,
                    order.Target, context.Policy.Mission ? (Faction?)null : context.Appearance[order.Target.Value], readout: context.Policy.Mission ? context.Readouts[order.Target.Value] : (Readout?)null));
            }
        }
    }
}
