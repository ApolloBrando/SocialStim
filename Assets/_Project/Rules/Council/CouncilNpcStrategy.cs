using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Initial scoped council policy; inference-depth and affinity policies extend this port separately.</summary>
    public sealed class CouncilNpcStrategy : ICouncilStrategy
    {
        public CouncilAction Speak(CouncilKnowledge k, IRandomSource random)
        {
            var previous = k.Events.LastOrDefault(e => e.Intent.HasValue);
            if (previous != null && previous.Target == k.Self && previous.Actor != k.Self)
            {
                if (previous.Intent == SpeechIntent.Accuse)
                    return new CouncilAction(CouncilActionKind.Defend, k.Self, ReasonType.Logic);
                if (previous.Intent == SpeechIntent.Question)
                    return new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.Answer);
            }
            var reported = k.Events.Where(e => e.Actor == k.Self && e.Readout.HasValue && e.Day == k.Day).Select(e => e.Target).ToList();
            var result = k.Results.LastOrDefault(r => r.Readout.HasValue && r.Night == k.Day - 1 && !reported.Contains(r.Target));
            if (result != null && k.OwnRole.CanClaimTruthfully)
                return new CouncilAction(CouncilActionKind.Claim, result.Target, ReasonType.Evidence, k.OwnRole.Id, result.Readout);
            bool claimed = k.Events.Any(e => e.Actor == k.Self && e.Kind == CouncilEventKind.Claim);
            if (!claimed && k.OwnRole.CanBluff && random.Next(100) < 25)
            {
                var bluffs = k.Catalog.Where(r => r.Faction == Faction.Town && r.CanClaimTruthfully).ToList();
                if (bluffs.Count > 0) return new CouncilAction(CouncilActionKind.Claim, claim: bluffs[random.Next(bluffs.Count)].Id, declaredLie: true);
            }
            if (!claimed && k.OwnRole.CanClaimTruthfully && k.Round >= 3 && random.Next(100) < 25)
                return new CouncilAction(CouncilActionKind.Claim, claim: k.OwnRole.Id);
            var targets = k.Characters.Where(c => c.Alive && c.Id != k.Self && !c.IsTeammate).ToList();
            if (targets.Count == 0) return new CouncilAction(CouncilActionKind.Pass);
            var suspect = targets.OrderByDescending(c => CouncilReasoning.Suspicion(k, c.Id)).ThenBy(c => c.Id.Value).First();
            int choice = random.Next(100);
            if (choice < 45)
            {
                var reasons = CouncilReasoning.Available(k, suspect.Id);
                return new CouncilAction(CouncilActionKind.Accuse, suspect.Id, reasons[random.Next(reasons.Count)]);
            }
            if (choice < 70) return new CouncilAction(CouncilActionKind.Chat, targets[random.Next(targets.Count)].Id, intent: SpeechIntent.Question);
            if (choice < 88) return new CouncilAction(CouncilActionKind.Defend, targets.OrderBy(c => CouncilReasoning.Suspicion(k, c.Id)).First().Id, ReasonType.GutFeeling);
            return new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.Banter);
        }

        public CharacterId? Vote(CouncilKnowledge k, IRandomSource random)
            => k.Characters.Where(c => c.Alive && c.Id != k.Self && !c.IsTeammate && (k.VoteTargets == null || k.VoteTargets.Contains(c.Id)))
                .OrderByDescending(c => CouncilReasoning.Suspicion(k, c.Id)).ThenBy(c => c.Id.Value)
                .Select(c => (CharacterId?)c.Id).FirstOrDefault();

        public CharacterId? NightTarget(CouncilKnowledge k, IRandomSource random)
        {
            if (k.Charges == 0 || k.OwnRole.Ability == AbilityKind.None) return null;
            var targets = k.Characters.Where(c => c.Alive && c.Id != k.Self
                && !(k.OwnRole.Faction == Faction.Antagonist && c.IsTeammate)).ToList();
            if (targets.Count == 0) return null;
            if (k.OwnRole.Ability == AbilityKind.Protect)
                return targets.OrderByDescending(c => c.ClaimedRole.HasValue ? 50 : 0)
                    .ThenBy(c => CouncilReasoning.Suspicion(k, c.Id)).ThenBy(c => c.Id.Value).First().Id;
            if (k.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !k.Results.Any(r => r.Target == c.Id && r.Readout.HasValue)).ToList();
                if (unknown.Count > 0) targets = unknown;
            }
            var suspect = targets.OrderByDescending(c => CouncilReasoning.Suspicion(k, c.Id)).ThenBy(c => c.Id.Value).First();
            if (k.OwnRole.Ability == AbilityKind.Attack && k.OwnRole.Faction == Faction.Town)
                return CouncilReasoning.Suspicion(k, suspect.Id) >= 75 ? suspect.Id : (CharacterId?)null;
            if (k.OwnRole.Ability == AbilityKind.Attack || k.OwnRole.Ability == AbilityKind.Block)
                return targets.OrderByDescending(c => c.ClaimedRole.HasValue
                    && k.Catalog.Any(r => r.Id == c.ClaimedRole.Value && r.Ability == AbilityKind.Investigate) ? 60 : 0)
                    .ThenByDescending(c => c.Trust).ThenBy(c => c.Id.Value).First().Id;
            return suspect.Id;
        }
    }
}
