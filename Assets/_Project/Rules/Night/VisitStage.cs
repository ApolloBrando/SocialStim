using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Tracks executed non-block visits and resource spending.</summary>
    public sealed class VisitStage : INightStage
    {
        /// <inheritdoc />
        public int Priority => 20;
        /// <inheritdoc />
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order) || context.Actor(order.Actor).Role.Ability == AbilityKind.Block) continue;
                context.Visits.Add(new Visit(order.Actor, order.Target.Value));
                if (context.Actor(order.Actor).Charges > 0) context.SpentCharges.Add(order.Actor);
            }
        }
    }
}
