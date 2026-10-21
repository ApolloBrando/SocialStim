using System;
using System.Linq;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Evidence-aware utilities over one observer's snapshot; feelings never establish a hidden role.</summary>
    internal static class SocialAssessment
    {
        internal static int CaseStrength(KnowledgeView view, KnownCharacter target)
        {
            if (target.KnownFaction == Faction.Town || target.IsTeammate) return 0;
            if (target.KnownFaction == Faction.Antagonist) return 100;
            if (view.OwnRole.Unique && target.ClaimedRole == view.OwnRole.Id) return 90;
            int strength = view.Evidence.Any(e => e.Kind == EvidenceKind.Contradiction && e.Actor == target.Id) ? 40 : 0;
            if (view.Evidence.Any(e => e.Kind == EvidenceKind.ClaimantLostAtNight && e.Actor == target.Id)) strength += 20;
            if (view.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == target.Id && r.Alignment == Faction.Antagonist)) strength += 65;
            var witnesses = view.Evidence.Where(e => e.Kind == EvidenceKind.InvestigationClaim && e.Target == target.Id
                && e.Alignment == Faction.Antagonist && e.Actor != view.Self).Select(e => e.Actor).Distinct();
            strength += witnesses.Sum(id => view.Characters.First(c => c.Id == id).TrustValue >= 5 ? 22 : 8);
            var casualties = view.Evidence.Where(e => e.Kind == EvidenceKind.Death && e.Target.HasValue).ToList();
            if (view.PrivateResults.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == target.Id
                && casualties.Any(d => d.Target == r.Subject && d.Day == r.Night))) strength += 18;
            strength += Math.Min(24, view.Evidence.Where(e => e.Kind == EvidenceKind.NightActionClaim && e.NightReport != null && e.Actor != target.Id
                && e.NightReport.Results.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == target.Id
                    && casualties.Any(d => d.Target == r.Subject && d.Day == e.Night)))
                .Select(e => e.Actor).Distinct().Count() * 12);
            var revealedEvil = view.Evidence.Where(e => e.Kind == EvidenceKind.RoleReveal && e.Alignment == Faction.Antagonist)
                .Select(e => e.Target ?? e.Actor).ToList();
            // Corroborated historical choices are stronger than the number of people repeating a rumor.
            strength += Math.Min(24, view.Evidence.Where(e => e.Actor == target.Id && e.Target.HasValue && revealedEvil.Contains(e.Target.Value)
                && (e.Intent == SpeechIntent.Vouch || e.Kind == EvidenceKind.Judgement && e.Verdict == Verdict.Innocent))
                .Select(e => e.Target.Value).Distinct().Count() * 12);
            return Math.Min(100, strength);
        }

        internal static int Threat(KnowledgeView view, KnownCharacter target)
        {
            if (target.KnownFaction == Faction.Town || target.IsTeammate) return 0;
            if (target.KnownFaction == Faction.Antagonist) return 100;
            float probability = target.FactionEstimate == null ? target.SuspicionValue * 10 : target.FactionEstimate.Antagonist / 100f;
            float score = target.SuspicionValue * 4 + probability * 0.6f;
            score += CaseStrength(view, target) * 0.3f;
            if (view.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == target.Id && r.Alignment == Faction.Town)) score -= 25;
            return Math.Max(0, Math.Min(100, (int)Math.Round(score)));
        }

        internal static KnownCharacter Suspect(KnowledgeView view, IRandomSource random, bool nomination = false)
        {
            var candidates = view.Characters.Where(c => c.Alive && c.Id != view.Self && !c.IsTeammate
                && (!nomination || view.NominationCandidates == null || view.NominationCandidates.Contains(c.Id))
                && (view.OwnRole.Faction != Faction.Town || c.KnownFaction != Faction.Town)).OrderBy(c => c.Id.Value).ToList();
            if (candidates.Count == 0) return null;
            EvidenceRecord last = view.Evidence.LastOrDefault(e => e.Actor == view.Self
                && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect || e.Intent == SpeechIntent.CallForTrial));
            var scores = candidates.Select(c => new {
                Target = c,
                Score = Threat(view, c) + random.Next(9)
                    - (last != null && last.Day == view.Day && last.Target == c.Id && CaseStrength(view, c) < 40 ? 12 : 0)
                    + (view.OwnRole.StealsParityWin && c.FactionEstimate != null ? c.FactionEstimate.Town / 500 : 0)
                    + (view.OwnRole.Faction == Faction.Antagonist ? SocialNpcStrategy.PublicThreat(view, c) / 4 : 0)
            }).ToList();
            int best = scores.Max(c => c.Score);
            var tied = scores.Where(c => c.Score == best).ToList();
            return tied[random.Next(tied.Count)].Target;
        }

        internal static Verdict Judge(KnowledgeView view, KnownCharacter target, int threshold)
        {
            if (target.IsTeammate || view.OwnRole.Faction == Faction.Town && target.KnownFaction == Faction.Town) return Verdict.Innocent;
            if (view.OwnRole.Faction == Faction.Antagonist) return Verdict.Guilty;
            int threat = Threat(view, target), evidence = CaseStrength(view, target);
            if (view.OwnRole.Faction != Faction.Town)
                return threat >= threshold - 10 || target.ClaimedRole.HasValue ? Verdict.Guilty : Verdict.Abstain;
            if (evidence >= 60 || evidence >= 22 && threat >= threshold || threat >= 80 && evidence >= 12) return Verdict.Guilty;
            return threat < 35 || view.PrivateResults.Any(r => r.Target == target.Id && r.Kind == ReportKind.Investigation
                && r.Alignment == Faction.Town) ? Verdict.Innocent : Verdict.Abstain;
        }
    }
}
