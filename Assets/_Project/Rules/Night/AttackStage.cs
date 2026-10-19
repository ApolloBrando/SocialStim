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
                context.Attacked.Add(order.Target.Value);
                bool fatal = role.Attack > context.Defense[order.Target.Value]
                    && !context.Actor(order.Target.Value).Role.Flags.HasFlag(RoleFlags.NightImmune);
                if (fatal && context.Policy.Mission && context.Heals.TryGetValue(order.Target.Value, out int tokens) && tokens > 0)
                { context.Heals[order.Target.Value] = tokens - 1; fatal = false; }
                if (fatal)
                {
                    context.Deaths.Add(order.Target.Value);
                    if (context.Policy.Mission && context.Policy.Backlash && role.Flags.HasFlag(RoleFlags.Backlash)
                        && context.Actor(order.Target.Value).Role.Faction == Faction.Town)
                        context.BacklashScheduled.Add(order.Actor);
                }
                else context.Reports.Add(new NightReport(order.Actor, ReportKind.AttackFailed, order.Target));
            }
        }
    }
}
