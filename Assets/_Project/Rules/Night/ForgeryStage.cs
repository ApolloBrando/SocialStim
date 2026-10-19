using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Unblocked forgeries frame targets as Antagonist for this night only.</summary>
    public sealed class ForgeryStage : INightStage
    {
        public int Priority => 15;
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order) || context.Actor(order.Actor).Role.Ability != AbilityKind.Forge) continue;
                context.Appearance[order.Target.Value] = Faction.Antagonist;
                context.Reports.Add(new NightReport(order.Actor, ReportKind.ForgeryApplied, order.Target));
            }
        }
    }
}
