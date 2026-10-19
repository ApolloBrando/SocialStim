using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Coordinates one authorized team kill, simultaneous resolution, resource updates and scoped results.</summary>
    internal sealed class CouncilNightDirector
    {
        private readonly CouncilWorld _world;
        private readonly ICouncilStrategy _strategy;
        private readonly NightResolver _resolver;
        internal CouncilNightDirector(CouncilWorld world, ICouncilStrategy strategy)
        {
            _world = world; _strategy = strategy;
            _resolver = new NightResolver(policy: new NightPolicy(true, world.Options.SelfHeal, world.Options.Backlash, world.Options.NotifyLull));
        }

        internal void Promote()
        {
            var id = EvilSuccession.Successor(_world.Living.Select(a => a.Night()).ToList(), _world.Options.PromoteEvil);
            if (!id.HasValue) return;
            var actor = _world.Actor(id.Value);
            actor.Role = CouncilWorld.CopyRole(actor.Role, AbilityKind.Attack, 1, actor.Role.Flags);
            actor.Charges = -1;
            foreach (var teammate in _world.Living.Where(a => a.Role.Faction == Faction.Antagonist))
                _world.Emit(CouncilEventKind.Promotion, actor.Name + " carries the team attack.", actor.Id, recipient: teammate.Id);
        }

        internal void Resolve(CharacterId? playerTarget)
        {
            var actors = _world.Living.Select(a => a.Night()).ToList();
            var policy = new NightPolicy(true, _world.Options.SelfHeal, _world.Options.Backlash, _world.Options.NotifyLull);
            var orders = _world.Living.Select(actor => {
                var target = actor.Id == _world.Player.Id ? playerTarget : _strategy.NightTarget(_world.Knowledge(actor), _world.Decisions);
                var order = new NightOrder(actor.Id, target);
                return NightLegality.IsLegal(actor.Night(), order, actors, policy) ? order : new NightOrder(actor.Id, null);
            }).ToList();
            var killers = orders.Where(o => _world.Actor(o.Actor).Role.Faction == Faction.Antagonist
                && _world.Actor(o.Actor).Role.Ability == AbilityKind.Attack && _world.Actor(o.Actor).Charges != 0)
                .OrderBy(o => o.Actor.Value).ToList();
            if (killers.Count > 0)
            {
                var player = killers.FirstOrDefault(o => o.Actor == _world.Player.Id);
                var chosen = player ?? killers.Where(o => o.Target.HasValue).GroupBy(o => o.Target.Value)
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Value).Select(g => g.First()).FirstOrDefault() ?? killers[0];
                var carrier = player ?? killers[0];
                foreach (var killer in killers)
                {
                    orders.Remove(killer);
                    orders.Add(new NightOrder(killer.Actor, killer.Actor == carrier.Actor ? chosen.Target : null));
                }
                foreach (var support in _world.Living.Where(a => a.Role.Faction == Faction.Antagonist && a.Role.Ability != AbilityKind.Attack))
                {
                    var existing = orders.Single(o => o.Actor == support.Id);
                    var target = _strategy.NightTarget(_world.Knowledge(support).WithTeamTarget(chosen.Target), _world.Decisions);
                    var proposed = new NightOrder(support.Id, target);
                    orders.Remove(existing);
                    orders.Add(NightLegality.IsLegal(support.Night(), proposed, actors, policy) ? proposed : new NightOrder(support.Id, null));
                }
                foreach (var teammate in _world.Living.Where(a => a.Role.Faction == Faction.Antagonist))
                    _world.Emit(CouncilEventKind.PrivateResult, "Team: " + _world.Actor(carrier.Actor).Name + " → "
                        + (chosen.Target.HasValue ? _world.Actor(chosen.Target.Value).Name : "Pass"), carrier.Actor, chosen.Target, recipient: teammate.Id);
            }
            foreach (var order in orders)
            {
                if (order.Actor != _world.Player.Id) _world.Trace(_world.Actor(order.Actor), "Night", order.Target);
                _world.Emit(CouncilEventKind.NightOrder, _world.Actor(order.Actor).Name + " → "
                    + (order.Target.HasValue ? _world.Actor(order.Target.Value).Name : "Pass"), order.Actor, order.Target, recipient: order.Actor);
            }
            var result = _resolver.Resolve(actors, orders);
            foreach (var actor in _world.Actors) actor.BacklashDue = false;
            foreach (var id in result.SpentCharges) _world.Actor(id).Charges--;
            foreach (var id in result.SelfHealsSpent) _world.Actor(id).SelfHealUsed = true;
            foreach (var id in result.BacklashScheduled) _world.Actor(id).BacklashDue = true;
            foreach (var id in result.Deaths)
            {
                var actor = _world.Actor(id); actor.Alive = false;
                _world.Emit(CouncilEventKind.Death, actor.Name + " died overnight.", target: id);
                if (_world.Options.RevealRoles)
                    _world.Emit(CouncilEventKind.RoleReveal, actor.Name + " · " + actor.Role.Name, target: id, reason: Dialogue.ReasonType.NightPattern, claim: actor.Role.Id);
            }
            if (result.Deaths.Count == 0) _world.Emit(CouncilEventKind.Morning, "Everyone made it through.");
            foreach (var report in result.Reports)
            {
                var recipient = _world.Actor(report.Recipient);
                if (!recipient.Alive) continue;
                var delivered = new NightReport(report.Recipient, report.Kind, report.Target, report.Alignment,
                    _world.Day, report.Subject, report.Readout);
                recipient.Results.Add(delivered);
                _world.Emit(CouncilEventKind.PrivateResult, Describe(delivered), recipient.Id, report.Target,
                    readout: report.Readout, recipient: recipient.Id);
            }
            Promote(); _world.Phase = CouncilPhase.Morning;
        }

        internal string Describe(NightReport report)
        {
            string target = report.Target.HasValue ? _world.Actor(report.Target.Value).Name : "";
            switch (report.Kind)
            {
                case ReportKind.Investigation: return target + " · " + report.Readout?.ToString().ToUpperInvariant();
                case ReportKind.Blocked: return "You were lulled. Your action failed.";
                case ReportKind.Targeted: return "You survived an attack.";
                case ReportKind.AttackFailed: return "Your attack on " + target + " failed.";
                case ReportKind.PatientAttacked: return target + " was attacked.";
                case ReportKind.PatientSafe: return target + " wasn't attacked.";
                case ReportKind.BacklashPending: return "Backlash will take you next night.";
                case ReportKind.ForgeryApplied: return "Your forgery took effect.";
                case ReportKind.VisitorSeen: return target + " visited " + _world.Actor(report.Subject.Value).Name + ".";
                default: return "No other visitors seen.";
            }
        }
    }
}
