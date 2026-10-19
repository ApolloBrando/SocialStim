using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Accepted blocks apply simultaneously, including mutual blocks.</summary>
    public sealed class RoleBlockStage : INightStage
    {
        /// <inheritdoc />
        public int Priority => 10;
        /// <inheritdoc />
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!order.Target.HasValue || context.Actor(order.Actor).Role.Ability != AbilityKind.Block) continue;
                context.Blocked.Add(order.Target.Value);
                context.Visits.Add(new Visit(order.Actor, order.Target.Value));
                if (context.Actor(order.Actor).Charges > 0) context.SpentCharges.Add(order.Actor);
            }
            foreach (NightActor actor in context.Actors)
                if (context.Blocked.Contains(actor.Id)) context.Reports.Add(new NightReport(actor.Id, ReportKind.Blocked));
        }
    }
}
