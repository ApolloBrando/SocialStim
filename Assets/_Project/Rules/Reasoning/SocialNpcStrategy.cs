using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Stateless utility strategy combining observer-local evidence, traits, and exposure.</summary>
    public sealed class SocialNpcStrategy : INpcStrategy
    {
        private readonly ReasoningSettings _settings;

        /// <summary>Creates a strategy with immutable authored tuning.</summary>
        public SocialNpcStrategy(ReasoningSettings settings = null) { _settings = settings ?? new ReasoningSettings(); }

        /// <inheritdoc />
        public GameCommand Speak(KnowledgeView knowledge, IRandomSource random)
        {
            var candidates = new List<(GameCommand command, int utility)>();
            KnownCharacter self = knowledge.Characters.First(c => c.Id == knowledge.Self);
            KnownCharacter suspect = Suspect(knowledge);
            KnownCharacter trusted = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self)
                .OrderByDescending(c => c.Trust).ThenBy(c => c.Id.Value).FirstOrDefault();
            bool deceptive = knowledge.OwnRole.Faction == Faction.Antagonist || knowledge.Personality.HasFlag(Personality.Deceptive);
            RoleSpec decoy = knowledge.RoleCatalog.Where(r => r.Faction == Faction.Town)
                .OrderByDescending(r => r.Ability == AbilityKind.Investigate).ThenBy(r => r.Id.Value, System.StringComparer.Ordinal).FirstOrDefault();
            RoleId claim = self.ClaimedRole ?? (deceptive && decoy != null ? decoy.Id : knowledge.OwnRole.Id);
            candidates.Add((GameCommand.Speak(SpeechIntent.StaySilent), 8));
            candidates.Add((GameCommand.Speak(SpeechIntent.ExpressFear), 12));
            candidates.Add((GameCommand.Speak(SpeechIntent.Defend), knowledge.SelfExposure >= _settings.ExposureThreshold ? 75 : 4));
            candidates.Add((GameCommand.Speak(SpeechIntent.ClaimRole, role: claim),
                self.ClaimedRole.HasValue ? 3 : 18 + (deceptive ? _settings.DeceptionBonus : 0)));
            if (knowledge.Evidence.Any(e => e.Kind == EvidenceKind.Death && e.Day >= knowledge.Day - 1))
                candidates.Add((GameCommand.Speak(SpeechIntent.ReactToDeath), 24));
            if (knowledge.Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal && e.Day >= knowledge.Day - 1))
                candidates.Add((GameCommand.Speak(SpeechIntent.ReactToRoleReveal), 26));

            NightReport result = knowledge.PrivateResults.LastOrDefault(r => r.Kind == ReportKind.Investigation
                && r.Night == knowledge.Day - 1 && knowledge.Characters.Any(c => c.Alive && c.Id == r.Target)
                && !HasReported(knowledge, r.Target.Value));
            if (result != null) candidates.Add((GameCommand.Speak(SpeechIntent.ReportInvestigation, result.Target, faction: result.Alignment), 90));
            if (suspect != null)
            {
                int accusation = Threat(suspect) / 2 + (knowledge.Personality.HasFlag(Personality.Aggressive) ? _settings.AggressiveBonus : 0);
                candidates.Add((GameCommand.Speak(SpeechIntent.Accuse, suspect.Id), accusation));
                if (Threat(suspect) >= 40) candidates.Add((GameCommand.Speak(SpeechIntent.CallForTrial, suspect.Id), accusation + 3));
                candidates.Add((GameCommand.Speak(SpeechIntent.Disagree, suspect.Id), 10 + accusation / 3));
                if (knowledge.SelfExposure >= _settings.ExposureThreshold && deceptive)
                    candidates.Add((GameCommand.Speak(SpeechIntent.Deflect, suspect.Id), 65 + _settings.DeceptionBonus));
                if (knowledge.Day > 1 && deceptive && !HasReported(knowledge, suspect.Id)
                    && knowledge.RoleCatalog.Any(r => r.Id == claim && r.Ability == AbilityKind.Investigate))
                    candidates.Add((GameCommand.Speak(SpeechIntent.ReportInvestigation, suspect.Id, faction: Faction.Antagonist), 45 + _settings.DeceptionBonus));
            }
            if (trusted != null)
            {
                int loyalty = knowledge.Personality.HasFlag(Personality.Loyal) ? _settings.LoyaltyBonus : 0;
                candidates.Add((GameCommand.Speak(SpeechIntent.Vouch, trusted.Id), 10 + loyalty + trusted.Trust / 6));
                candidates.Add((GameCommand.Speak(SpeechIntent.Agree, trusted.Id), 8 + loyalty + trusted.Trust / 8));
            }
            EvidenceRecord last = knowledge.Evidence.LastOrDefault(e => e.Actor == knowledge.Self && e.Intent.HasValue);
            return candidates.Select(c => (c.command, score: c.utility + random.Next(9)
                - (last != null && last.Intent == c.command.Intent && last.Target == c.command.Target ? 24 : 0)))
                .OrderByDescending(c => c.score).First().command;
        }

        /// <inheritdoc />
        public CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random)
        {
            KnownCharacter suspect = Suspect(knowledge);
            int threshold = 28 + (knowledge.Personality.HasFlag(Personality.Cautious) ? _settings.CautiousThreshold : 0);
            return suspect != null && Threat(suspect) >= threshold ? suspect.Id : (CharacterId?)null;
        }

        /// <inheritdoc />
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random)
        {
            KnownCharacter target = knowledge.Characters.First(c => c.Id == defendant);
            if (target.IsTeammate) return Verdict.Innocent;
            int threshold = 35 + (knowledge.Personality.HasFlag(Personality.Cautious) ? _settings.CautiousThreshold : 0);
            if (knowledge.Personality.HasFlag(Personality.Loyal) && target.Trust > 60) threshold += _settings.LoyaltyBonus;
            if (knowledge.OwnRole.Faction == Faction.Antagonist) threshold -= 15;
            return Threat(target) >= threshold ? Verdict.Guilty : Verdict.Innocent;
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.Charges == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.SelfProtect)
                return knowledge.SelfExposure >= _settings.ExposureThreshold || random.Next(100) < 45 ? knowledge.Self : (CharacterId?)null;
            var targets = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self && !c.IsTeammate).ToList();
            if (targets.Count == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
                return targets.OrderByDescending(c => PublicThreat(knowledge, c)).ThenBy(c => c.Id.Value).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Protect)
                return targets.OrderByDescending(c => c.Trust + (100 - Threat(c))).ThenBy(c => c.Id.Value).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !knowledge.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == c.Id)).ToList();
                if (unknown.Count > 0) targets = unknown;
            }
            return targets.OrderByDescending(Threat).ThenBy(c => c.Id.Value).First().Id;
        }

        internal static int PublicThreat(KnowledgeView knowledge, KnownCharacter character)
        {
            RoleSpec claim = character.ClaimedRole.HasValue ? knowledge.RoleCatalog.FirstOrDefault(r => r.Id == character.ClaimedRole.Value) : null;
            return character.Trust + (claim != null && claim.Ability == AbilityKind.Investigate ? 35 : 0);
        }

        private static bool HasReported(KnowledgeView knowledge, CharacterId target) => knowledge.Evidence.Any(e =>
            e.Actor == knowledge.Self && e.Kind == EvidenceKind.InvestigationClaim && e.Target == target && e.Night == knowledge.Day - 1);
        private static int Threat(KnownCharacter character) => (character.Suspicion * 2 + (character.FactionEstimate?.Antagonist ?? character.Suspicion * 100) / 100) / 3;
        private static KnownCharacter Suspect(KnowledgeView knowledge) => knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self && !c.IsTeammate)
            .OrderByDescending(Threat).ThenBy(c => c.Id.Value).FirstOrDefault();
    }
}
