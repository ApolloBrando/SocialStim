using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Validates the locked night then runs an explicitly composed ordered pipeline.</summary>
    public sealed class NightResolver
    {
        private readonly IReadOnlyList<INightStage> _stages;

        /// <summary>Creates the initial pipeline or a custom set with unique priorities.</summary>
        public NightResolver(IEnumerable<INightStage> stages = null)
        {
            var ordered = (stages ?? new INightStage[] { new RoleBlockStage(), new ForgeryStage(), new VisitStage(),
                new ProtectionStage(), new AttackStage(), new WatchStage(), new InvestigationStage() }).OrderBy(s => s.Priority).ToList();
            if (ordered.Count == 0 || ordered.Select(s => s.Priority).Distinct().Count() != ordered.Count)
                throw new ArgumentException("Night stages require unique priorities.", nameof(stages));
            _stages = ordered.AsReadOnly();
        }

        /// <summary>Resolves one batch; rejects duplicates, missing actors, illegal targets and multiple team kills.</summary>
        public NightResolution Resolve(IEnumerable<NightActor> actors, IEnumerable<NightOrder> orders)
        {
            var cast = actors.OrderBy(a => a.Id.Value).ToList().AsReadOnly();
            var locked = orders.OrderBy(a => a.Actor.Value).ToList().AsReadOnly();
            if (cast.Select(a => a.Id).Distinct().Count() != cast.Count ||
                locked.Select(o => o.Actor).Distinct().Count() != locked.Count)
                throw new ArgumentException("Duplicate night actor or order.");
            foreach (NightOrder order in locked)
                if (!NightLegality.IsLegal(cast.FirstOrDefault(a => a.Id == order.Actor), order, cast))
                    throw new ArgumentException("Illegal night order.");
            if (locked.Count(o => o.Target.HasValue && cast.First(a => a.Id == o.Actor).Role.Faction == Faction.Antagonist
                && cast.First(a => a.Id == o.Actor).Role.Ability == AbilityKind.Attack) > 1)
                throw new ArgumentException("The antagonist team has only one kill per night.");
            var context = new NightContext(cast, locked);
            foreach (INightStage stage in _stages) stage.Resolve(context);
            foreach (NightActor actor in cast.Where(a => a.GuiltDue)) context.Deaths.Add(actor.Id);
            return new NightResolution(context);
        }
    }

    /// <summary>One ordered rule stage; ordering is supplied at composition time.</summary>
    public interface INightStage
    {
        /// <summary>Stable stage priority; duplicate priorities are rejected.</summary>
        int Priority { get; }
        /// <summary>Resolves against authoritative night context.</summary>
        void Resolve(NightContext context);
    }

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
                context.Defense[order.Target.Value] = Math.Max(context.Defense[order.Target.Value], role.Protection);
                context.Reports.Add(new NightReport(order.Actor, ReportKind.ProtectionApplied, order.Target));
            }
        }
    }

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
                    order.Target, context.Appearance[order.Target.Value]));
            }
        }
    }
}
