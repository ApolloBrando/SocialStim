using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Scoped expected-utility policy with distinct faction objectives and bounded human mistakes.</summary>
    public sealed class CouncilNpcStrategy : ICouncilStrategy
    {
        /// <inheritdoc />
        public CouncilAction Speak(CouncilKnowledge k, IRandomSource random)
        {
            var last = k.Events.LastOrDefault(e => e.Intent.HasValue);
            bool addressed = last != null && last.Target == k.Self && last.Actor != k.Self;
            bool claimed = k.Characters.Single(c => c.Id == k.Self).ClaimedRole.HasValue;
            double exposure = k.Inference?.Exposure ?? 0;
            var results = k.Results.Where(r => r.Night == k.Day - 1
                && (r.Readout.HasValue || r.Kind == ReportKind.PatientAttacked || r.Kind == ReportKind.PatientSafe)
                && !k.Events.Any(e => e.Actor == k.Self && e.Day == k.Day && e.Kind == CouncilEventKind.Report && e.Target == r.Target)).ToList();
            var critical = results.FirstOrDefault(r => r.Readout == Readout.Corrupt || r.Readout == Readout.Anomaly || r.Kind == ReportKind.PatientAttacked);
            var report = critical ?? (k.Round >= 2 || addressed ? results.FirstOrDefault() : null);
            if (report != null && k.OwnRole.CanClaimTruthfully)
                return new CouncilAction(claimed ? CouncilActionKind.Report : CouncilActionKind.Claim, report.Target, ReasonType.Evidence,
                    claimed ? (RoleId?)null : k.OwnRole.Id, report.Readout,
                    patientAttacked: report.Readout.HasValue ? (bool?)null : report.Kind == ReportKind.PatientAttacked);
            if (!claimed && k.OwnRole.CanClaimTruthfully && (addressed || exposure > 0.65))
                return new CouncilAction(CouncilActionKind.Claim, claim: k.OwnRole.Id);
            if (addressed)
                return last.Intent == SpeechIntent.Question ? new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.Answer)
                    : new CouncilAction(CouncilActionKind.Defend, k.Self, ReasonType.Logic);

            var targets = Targets(k, false).ToList();
            if (targets.Count == 0) return new CouncilAction(CouncilActionKind.Pass);
            var ranked = targets.OrderByDescending(c => VoteUtility(k, c)).ThenBy(c => c.Id.Value).ToList();
            var suspect = ranked[0];
            if (random.Next(100) < k.Policy.MistakePercent * (1.3 - k.Skill(k.Self).Get(StatKind.Intuition) / 20.0) && ranked.Count > 1) suspect = ranked[random.Next(Math.Min(3, ranked.Count))];
            if (!claimed && k.OwnRole.CanBluff && (exposure > 0.45 || k.Round >= 2 && random.Next(100) < 14))
            {
                var bluffs = k.Catalog.Where(r => r.Faction == Faction.Town && r.CanClaimTruthfully
                    && (k.RoleCounts == null || k.RoleCounts.TryGetValue(r.Id, out int copies) && copies > 0))
                    .OrderBy(r => k.Characters.Count(c => c.ClaimedRole == r.Id))
                    .ThenByDescending(r => r.Ability == AbilityKind.Investigate && k.Skill(k.Self).Get(StatKind.Performance) >= 6)
                    .ThenBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
                if (bluffs.Count > 0)
                    return new CouncilAction(CouncilActionKind.Claim, claim: bluffs[0].Id, declaredLie: bluffs[0].Id != k.OwnRole.Id);
            }
            int aggression = k.Traits.HasFlag(Personality.Aggressive) ? 14 : k.Traits.HasFlag(Personality.Cautious) ? -10 : 0;
            int chance = 36 + aggression + (int)((k.Inference?.Tension ?? 0) * 20);
            if (k.OwnRole.Objective == RoleObjective.StealParity) chance -= 18;
            if (random.Next(100) < chance)
                return new CouncilAction(CouncilActionKind.Accuse, suspect.Id, BestReason(k, suspect.Id),
                    declaredLie: k.OwnRole.CanBluff && Belief(k, suspect.Id)?.Town > 0.7);
            var trusted = targets.Where(c => Belief(k, c.Id)?.Hostile < 0.40)
                .OrderByDescending(c => Belief(k, c.Id).Affinity + Belief(k, c.Id).Credibility).ThenBy(c => c.Id.Value).FirstOrDefault();
            if (trusted != null && random.Next(100) < 35)
                return new CouncilAction(CouncilActionKind.Defend, trusted.Id, ReasonType.Logic);
            if (random.Next(100) < 65)
                return new CouncilAction(CouncilActionKind.Chat, suspect.Id, intent: SpeechIntent.Question);
            return new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.Banter);
        }

        /// <inheritdoc />
        public CharacterId? Vote(CouncilKnowledge k, IRandomSource random)
        {
            var forecast = new CouncilVoteForecast(k);
            var targets = Targets(k, true).OrderByDescending(c => VoteUtility(k, c) + forecast.Utility(c.Id) * k.Policy.Coordination)
                .ThenBy(c => c.Id.Value).ToList();
            if (targets.Count == 0) return null;
            int index = random.Next(100) < k.Policy.MistakePercent * (1.3 - k.Skill(k.Self).Get(StatKind.Intuition) / 20.0) ? random.Next(Math.Min(3, targets.Count)) : 0;
            return targets[index].Id;
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(CouncilKnowledge k, IRandomSource random)
        {
            if (k.Charges == 0 || k.OwnRole.Ability == AbilityKind.None) return null;
            var targets = k.Characters.Where(c => c.Alive && c.Id != k.Self
                && !(k.OwnRole.Faction == Faction.Antagonist && c.IsTeammate)).ToList();
            if (k.OwnRole.Ability == AbilityKind.Protect)
            {
                if (k.SelfHealAllowed && !k.SelfHealUsed && k.OwnRole.Flags.HasFlag(RoleFlags.SelfHeal))
                    targets.Add(k.Characters.Single(c => c.Id == k.Self));
                return targets.OrderByDescending(c => Threat(k, c) * (1 - (Belief(k, c.Id)?.Hostile ?? 0.35))
                    + (Belief(k, c.Id)?.Affinity ?? 0) * 0.3).ThenBy(c => c.Id.Value).Select(c => (CharacterId?)c.Id).FirstOrDefault();
            }
            if (targets.Count == 0) return null;
            if (k.OwnRole.Ability == AbilityKind.Forge)
            {
                var teammate = k.Characters.Where(c => c.Alive && c.IsTeammate && c.Id != k.TeamTarget)
                    .OrderByDescending(c => PublicPressure(k, c.Id)).ThenBy(c => c.Id.Value).FirstOrDefault();
                if (teammate != null && PublicPressure(k, teammate.Id) > 0.6) return teammate.Id;
                return targets.Where(c => c.Id != k.TeamTarget)
                    .OrderByDescending(c => PublicPressure(k, c.Id) + (Belief(k, c.Id)?.Town ?? 0.5))
                    .ThenBy(c => c.Id.Value).Select(c => (CharacterId?)c.Id).FirstOrDefault();
            }
            if (k.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !k.Results.Any(r => r.Target == c.Id && r.Readout.HasValue)).ToList();
                if (unknown.Count > 0) targets = unknown;
                return targets.OrderByDescending(c => Uncertainty(k, c.Id) * (0.6 + Threat(k, c)))
                    .ThenBy(c => c.Id.Value).First().Id;
            }
            if (k.OwnRole.Ability == AbilityKind.Attack && k.OwnRole.Faction == Faction.Town)
            {
                var suspect = targets.Where(c => c.RevealedFaction != Faction.Town)
                    .OrderByDescending(c => Belief(k, c.Id)?.Hostile ?? 0).ThenBy(c => c.Id.Value).FirstOrDefault();
                return suspect != null && (Belief(k, suspect.Id)?.Hostile ?? 0) >= k.Policy.ShotThreshold ? suspect.Id : (CharacterId?)null;
            }
            if (k.OwnRole.Objective == RoleObjective.LastSurvivor)
            {
                if (targets.Count > 4 && (k.Inference?.Exposure ?? 0) > 0.6 && random.Next(100) < 35) return null;
                return targets.OrderByDescending(c => Threat(k, c) * (1 - InfinitumChance(k, c.Id)) + (Belief(k, c.Id)?.Evil ?? 0) * 0.4)
                    .ThenBy(c => c.Id.Value).First().Id;
            }
            var ordered = targets.Where(c => k.OwnRole.Ability != AbilityKind.Block || c.Id != k.TeamTarget)
                .OrderByDescending(c => Threat(k, c) * (1 - InfinitumChance(k, c.Id)))
                .ThenBy(c => c.Id.Value).ToList();
            if (ordered.Count == 0) return null;
            int mistake = random.Next(100) < k.Policy.MistakePercent * (1.3 - k.Skill(k.Self).Get(StatKind.Intuition) / 20.0) ? random.Next(Math.Min(3, ordered.Count)) : 0;
            return ordered[mistake].Id;
        }

        internal static CouncilBelief Belief(CouncilKnowledge k, CharacterId target)
            => k.Inference != null && k.Inference.Beliefs.TryGetValue(target, out CouncilBelief value) ? value : null;
        internal static double Threat(CouncilKnowledge k, Reasoning.KnownCharacter target)
        {
            var claim = target.ClaimedRole.HasValue ? k.Catalog.FirstOrDefault(r => r.Id == target.ClaimedRole.Value) : null;
            double power = claim?.Ability == AbilityKind.Investigate ? 1.3 : claim?.Ability == AbilityKind.Protect ? 1.0 : claim?.Ability == AbilityKind.Attack ? 0.9 : 0;
            var stats = k.Skill(target.Id);
            return 0.3 + power * (Belief(k, target.Id)?.Credibility ?? 0.5)
                + stats.Get(StatKind.Charisma) / 15.0 + stats.Get(StatKind.Logic) / 25.0 - stats.Get(StatKind.Stealth) / 35.0;
        }
        internal static double PublicPressure(CouncilKnowledge k, CharacterId target)
            => k.Events.Where(e => e.Kind == CouncilEventKind.Vote || e.Intent == SpeechIntent.Accuse)
                .GroupBy(e => e.Actor).Select(g => g.Last()).Count(e => e.Target == target)
                / (double)Math.Max(1, k.Characters.Count(c => c.Alive) - 1);
        private static IEnumerable<Reasoning.KnownCharacter> Targets(CouncilKnowledge k, bool vote)
            => k.Characters.Where(c => c.Alive && c.Id != k.Self && !c.IsTeammate
                && (k.OwnRole.Faction != Faction.Town || c.RevealedFaction != Faction.Town)
                && (!vote || k.VoteTargets == null || k.VoteTargets.Contains(c.Id)));
        private static double VoteUtility(CouncilKnowledge k, Reasoning.KnownCharacter target)
        {
            var belief = Belief(k, target.Id);
            double hostile = belief?.Hostile ?? 0.35;
            double affinity = belief?.Affinity ?? 0;
            double objective = k.OwnRole.Faction == Faction.Town ? hostile * 2.5 - affinity * 0.4
                : k.OwnRole.Faction == Faction.Antagonist ? (belief?.Town ?? 0.6) * 1.8 + (belief?.Killer ?? 0.1) * 2.1
                : k.OwnRole.Objective == RoleObjective.StealParity ? (belief?.Town ?? 0.6) * 1.8 + (belief?.Killer ?? 0.1) * 3
                : Threat(k, target) * 0.7;
            double predicted = PublicPressure(k, target.Id);
            double survival = (k.Inference?.Exposure ?? 0) * predicted;
            double loyalty = k.Traits.HasFlag(Personality.Loyal) ? Math.Max(0, affinity) * 0.4 : 0;
            double bandwagon = k.Traits.HasFlag(Personality.Gullible) ? 0.8 : 0.4;
            return objective + predicted * bandwagon + survival - loyalty;
        }
        private static ReasonType BestReason(CouncilKnowledge k, CharacterId target)
            => CouncilReasoning.Available(k, target).OrderByDescending(r => CouncilSocialEvidence.Strength(k, target, r))
                .ThenBy(r => r).First();
        private static double Uncertainty(CouncilKnowledge k, CharacterId target)
        {
            var belief = Belief(k, target);
            return belief == null ? 1 : -(new[] { belief.Town, belief.Evil, belief.Neutral }.Where(p => p > 0).Sum(p => p * Math.Log(p)));
        }
        private static double InfinitumChance(CouncilKnowledge k, CharacterId target)
        {
            var belief = Belief(k, target); if (belief == null) return 0;
            return k.Catalog.Where(r => r.Objective == RoleObjective.StealParity).Sum(r => belief.Roles[r.Id]);
        }
    }
}
