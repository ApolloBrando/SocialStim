using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Fractional ballot forecast from public stances; no unrevealed current ballot or foreign belief is read.</summary>
    internal sealed class CouncilVoteForecast
    {
        private readonly CouncilKnowledge _knowledge;
        private readonly Dictionary<CharacterId, double> _predicted;
        internal CouncilVoteForecast(CouncilKnowledge knowledge)
        {
            _knowledge = knowledge;
            var living = knowledge.Characters.Where(c => c.Alive).ToList();
            _predicted = living.ToDictionary(c => c.Id, c => 0.0);
            foreach (var voter in living.Where(c => c.Id != knowledge.Self))
            {
                var stance = knowledge.Events.LastOrDefault(e => e.Actor == voter.Id && e.Target.HasValue
                    && (e.Intent == SpeechIntent.Accuse || e.Kind == CouncilEventKind.Vote));
                var choices = living.Where(c => c.Id != voter.Id && (knowledge.VoteTargets == null || knowledge.VoteTargets.Contains(c.Id))).ToList();
                if (choices.Count == 0) continue;
                double total = choices.Sum(c => Weight(knowledge, c.Id, stance?.Target));
                foreach (var choice in choices) _predicted[choice.Id] += Weight(knowledge, choice.Id, stance?.Target) / total;
            }
        }
        internal double Utility(CharacterId ballot)
        {
            // Evaluate the extra ballot's change in probable plurality and tie exposure.
            var before = Chances(null); var after = Chances(ballot);
            double expected = 0;
            foreach (var target in after.Keys)
                expected += (after[target] - before[target]) * EliminationValue(target);
            double danger = after.TryGetValue(_knowledge.Self, out double selfRisk) ? selfRisk : 0;
            return expected * 2.5 - danger * (_knowledge.OwnRole.Faction == Faction.Town ? 0.4 : 1.2);
        }
        private Dictionary<CharacterId, double> Chances(CharacterId? ballot)
        {
            var candidates = _predicted.Keys.Where(id => _knowledge.VoteTargets == null || _knowledge.VoteTargets.Contains(id)).ToList();
            var weights = candidates.ToDictionary(id => id, id => Math.Exp(Math.Min(20, (_predicted[id] + (id == ballot ? 1 : 0)) * 2.5)));
            double total = weights.Values.Sum();
            return weights.ToDictionary(p => p.Key, p => p.Value / Math.Max(0.00001, total));
        }
        private double EliminationValue(CharacterId target)
        {
            var b = CouncilNpcStrategy.Belief(_knowledge, target);
            if (target == _knowledge.Self) return -1;
            if (_knowledge.Characters.Single(c => c.Id == target).IsTeammate) return -1;
            if (_knowledge.OwnRole.Faction == Faction.Town) return b?.Hostile ?? 0.35;
            if (_knowledge.OwnRole.Objective == RoleObjective.LastSurvivor) return 0.6 + (b?.Killer ?? 0);
            double aliveEvil = _knowledge.Characters.Where(c => c.Alive).Sum(c => CouncilNpcStrategy.Belief(_knowledge, c.Id)?.Evil ?? 0.25);
            double aliveTown = _knowledge.Characters.Where(c => c.Alive).Sum(c => CouncilNpcStrategy.Belief(_knowledge, c.Id)?.Town ?? 0.6);
            double parityUrgency = 1 / (1 + Math.Exp(aliveTown - aliveEvil - 1));
            return (b?.Town ?? 0.6) * (1 + parityUrgency) + (b?.Killer ?? 0) * 1.5;
        }
        private static double Weight(CouncilKnowledge k, CharacterId target, CharacterId? stance)
            => 0.3 + CouncilNpcStrategy.PublicPressure(k, target) + (target == stance ? 1.4 : 0);
    }
}
