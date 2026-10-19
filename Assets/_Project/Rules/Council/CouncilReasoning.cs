using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Rules.Council
{
    /// <summary>Public evidence predicates for player and NPC reason selection.</summary>
    public static class CouncilReasoning
    {
        public static bool HasReason(CouncilKnowledge knowledge, CharacterId target, ReasonType reason)
        {
            var events = knowledge.Events;
            switch (reason)
            {
                case ReasonType.None: case ReasonType.GutFeeling: case ReasonType.Logic: case ReasonType.Emotional: case ReasonType.Humor: return true;
                case ReasonType.VoteHistory: return events.Any(e => e.Kind == CouncilEventKind.Vote && e.Actor == target && e.Target.HasValue);
                case ReasonType.ClaimTiming: return events.Any(e => e.Kind == CouncilEventKind.Claim && e.Actor == target && (e.Day > 1 || e.Round > 2));
                case ReasonType.Silence: return knowledge.Round > 1 && !events.Any(e => e.Day == knowledge.Day && e.Actor == target && e.Intent.HasValue);
                case ReasonType.OverEagerness: return events.Count(e => e.Day == knowledge.Day && e.Actor == target && e.Intent.HasValue) > knowledge.Round + 2;
                case ReasonType.Retaliation: return events.Any(e => e.Actor == target && e.Target == knowledge.Self && e.Intent == SpeechIntent.Accuse);
                case ReasonType.Bandwagon: return events.Count(e => e.Target == target && e.Intent == SpeechIntent.Accuse) >= 2;
                case ReasonType.NightPattern: return events.Any(e => e.Kind == CouncilEventKind.Death && e.Day >= knowledge.Day - 1);
                case ReasonType.PowerRoleKilled: return events.Any(e => e.Kind == CouncilEventKind.Death && events.Any(r =>
                    r.Kind == CouncilEventKind.RoleReveal && r.Target == e.Target && r.Claim.HasValue
                    && knowledge.Catalog.Any(c => c.Id == r.Claim.Value && c.Ability != Roles.AbilityKind.None)));
                case ReasonType.ClaimConflict:
                    var claim = events.LastOrDefault(e => e.Actor == target && e.Kind == CouncilEventKind.Claim)?.Claim;
                    return claim.HasValue && knowledge.Catalog.Any(r => r.Id == claim.Value && r.Unique)
                        && events.Where(e => e.Kind == CouncilEventKind.Claim).GroupBy(e => e.Actor)
                            .Select(g => g.Last()).Count(e => e.Claim == claim) > 1;
                case ReasonType.Contradiction:
                    return events.Where(e => e.Actor == target && e.Kind == CouncilEventKind.Claim).Select(e => e.Claim).Distinct().Count() > 1
                        || events.Where(e => e.Actor == target && e.Readout.HasValue).GroupBy(e => new { e.Day, e.Target }).Any(g => g.Select(e => e.Readout).Distinct().Count() > 1);
                case ReasonType.DefendedEvil: return events.Any(e => e.Actor == target && e.Intent == SpeechIntent.Vouch
                    && events.Any(r => r.Kind == CouncilEventKind.RoleReveal && r.Target == e.Target && r.Claim.HasValue
                        && knowledge.Catalog.Any(c => c.Id == r.Claim.Value && c.Faction == Roles.Faction.Antagonist)));
                case ReasonType.ResultMismatch: return events.Any(e => e.Actor == target && e.Readout.HasValue
                    && events.Any(r => r.Kind == CouncilEventKind.RoleReveal && r.Target == e.Target && r.Claim.HasValue
                        && knowledge.Catalog.Any(c => c.Id == r.Claim.Value && c.Readout != e.Readout.Value)));
                case ReasonType.Evidence: return knowledge.Results.Any(r => r.Target == target);
                case ReasonType.Alibi: return events.Any(e => e.Actor == target && e.Kind == CouncilEventKind.Report);
                default: return false;
            }
        }

        public static IReadOnlyList<ReasonType> Available(CouncilKnowledge knowledge, CharacterId target)
            => System.Enum.GetValues(typeof(ReasonType)).Cast<ReasonType>().Where(r => r != ReasonType.None && HasReason(knowledge, target, r)).ToList().AsReadOnly();

        public static int Suspicion(CouncilKnowledge knowledge, CharacterId target)
        {
            if (knowledge.Inference != null && knowledge.Inference.Beliefs.TryGetValue(target, out CouncilBelief belief))
                return (int)System.Math.Round(belief.Hostile * 100);
            var known = knowledge.Characters.First(c => c.Id == target);
            return known.RevealedFaction == Roles.Faction.Town ? 0 : known.RevealedFaction == Roles.Faction.Antagonist ? 100 : 35;
        }
    }
}
