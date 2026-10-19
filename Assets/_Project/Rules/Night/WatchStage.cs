using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Reports actual visitors only to an unblocked watcher; no role or attack attribution.</summary>
    public sealed class WatchStage : INightStage
    {
        public int Priority => 45;
        public void Resolve(NightContext context)
        {
            foreach (NightOrder order in context.Orders)
            {
                if (!context.Executes(order) || context.Actor(order.Actor).Role.Ability != AbilityKind.Watch) continue;
                var visitors = context.Visits.Where(v => v.Target == order.Target.Value && v.Visitor != order.Actor)
                    .OrderBy(v => v.Visitor.Value).ToList();
                if (visitors.Count == 0)
                    context.Reports.Add(new NightReport(order.Actor, ReportKind.NoVisitors, order.Target, subject: order.Target));
                foreach (Visit visit in visitors)
                    context.Reports.Add(new NightReport(order.Actor, ReportKind.VisitorSeen, visit.Visitor, subject: order.Target));
            }
        }
    }
}
