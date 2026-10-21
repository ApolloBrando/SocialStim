using System;
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

        /// <summary>Optional public follow-up, using only this listener's permitted beliefs.</summary>
        public static GameCommand Reaction(KnowledgeView knowledge, CharacterId speaker, CharacterId subject,
            bool accusation, IRandomSource random)
        {
            if (knowledge.Self == speaker || knowledge.Self == subject) return null;
            KnownCharacter target = knowledge.Characters.First(c => c.Id == subject);
            KnownCharacter source = knowledge.Characters.First(c => c.Id == speaker);
            int threat = SocialAssessment.Threat(knowledge, target), evidence = SocialAssessment.CaseStrength(knowledge, target);
            bool supportPressure = !target.IsTeammate && target.KnownFaction != Faction.Town
                && (evidence >= 22 || threat >= 65 && source.TrustValue >= 6);
            if (knowledge.OwnRole.Faction == Faction.Antagonist && !target.IsTeammate && source.IsTeammate)
                supportPressure = true;
            bool supportDefense = target.IsTeammate || target.KnownFaction == Faction.Town
                || threat < 35 && target.TrustValue >= 6;
            if (!supportPressure && !supportDefense) return null;
            int chance = knowledge.Personality == Personality.Chatterbox ? 55
                : knowledge.Personality == Personality.Slow || knowledge.Personality == Personality.Grim ? 22 : 35;
            if (random.Next(100) >= chance) return null;
            return GameCommand.Speak(supportPressure == accusation ? SpeechIntent.Agree : SpeechIntent.Disagree, speaker);
        }

        /// <inheritdoc />
        public GameCommand Speak(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.CanGiveReport) return GameCommand.ReportLastAction();
            var candidates = new List<(GameCommand command, int utility)>();
            KnownCharacter suspect = SocialAssessment.Suspect(knowledge, random);
            KnownCharacter trusted = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self)
                .OrderByDescending(c => c.Trust).ThenBy(c => random.Next(1000)).FirstOrDefault();
            bool deceptive = knowledge.OwnRole.CanBluff;
            RoleSpec decoy = ClaimPolicy.Proactive(knowledge);
            candidates.Add((GameCommand.Speak(SpeechIntent.StaySilent), knowledge.OwnRole.StealsParityWin ? 25 : 18));
            candidates.Add((GameCommand.Speak(SpeechIntent.ExpressFear), 10));
            candidates.Add((GameCommand.Speak(SpeechIntent.Defend), knowledge.SelfExposure >= _settings.ExposureThreshold ? 75 : 4));
            // Town power roles keep cover until pressure or useful private evidence warrants a claim.
            if (decoy != null && (deceptive || knowledge.SelfExposure >= _settings.ExposureThreshold
                || knowledge.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Alignment == Faction.Antagonist)))
                candidates.Add((GameCommand.Speak(SpeechIntent.ClaimRole, role: decoy.Id),
                    28 + (deceptive ? _settings.DeceptionBonus : 35)));
            if (knowledge.Evidence.Any(e => e.Kind == EvidenceKind.Death && e.Day >= knowledge.Day - 1))
                candidates.Add((GameCommand.Speak(SpeechIntent.ReactToDeath), 24));
            if (knowledge.Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal && e.Day >= knowledge.Day - 1))
                candidates.Add((GameCommand.Speak(SpeechIntent.ReactToRoleReveal), 26));

            NightReport result = knowledge.PrivateResults.LastOrDefault(r => r.Kind == ReportKind.Investigation
                && r.Night == knowledge.Day - 1 && knowledge.Characters.Any(c => c.Alive && c.Id == r.Target)
                && !HasReported(knowledge, r.Target.Value));
            if (result != null && knowledge.LastNightAction == null) candidates.Add((GameCommand.Speak(SpeechIntent.ReportInvestigation, result.Target, faction: result.Alignment), 90));
            GameCommand fabrication = ClaimPolicy.FabricatedInvestigation(knowledge, random);
            if (fabrication != null && knowledge.LastNightAction == null) candidates.Add((fabrication, 45 + _settings.DeceptionBonus));
            if (suspect != null)
            {
                int threat = SocialAssessment.Threat(knowledge, suspect), evidence = SocialAssessment.CaseStrength(knowledge, suspect);
                int accusation = 10 + Math.Max(0, threat - 40) / 2 + evidence / 3
                    + ((knowledge.Personality == Personality.Brave || knowledge.Personality == Personality.Smug) ? _settings.AggressiveBonus / 3 : 0);
                if (knowledge.OwnRole.Faction == Faction.Antagonist) accusation += PublicThreat(knowledge, suspect) / 4;
                if (knowledge.OwnRole.Category == RoleCategory.NeutralEvil) accusation += 12;
                candidates.Add((GameCommand.Speak(SpeechIntent.Accuse, suspect.Id), accusation));
                if (evidence >= 22 && threat >= 50) candidates.Add((GameCommand.Speak(SpeechIntent.CallForTrial, suspect.Id), accusation + 3));
                if (knowledge.SelfExposure >= _settings.ExposureThreshold && deceptive)
                    candidates.Add((GameCommand.Speak(SpeechIntent.Deflect, suspect.Id), 65 + _settings.DeceptionBonus));
            }
            if (trusted != null)
            {
                int loyalty = (knowledge.Personality == Personality.Brave) ? _settings.LoyaltyBonus : 0;
                candidates.Add((GameCommand.Speak(SpeechIntent.Vouch, trusted.Id), 10 + loyalty + trusted.Trust / 6));
                EvidenceRecord statement = knowledge.Evidence.LastOrDefault(e => e.Day == knowledge.Day && e.Actor == trusted.Id
                    && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Defend || e.Intent == SpeechIntent.Vouch || e.Intent == SpeechIntent.Deflect));
                if (statement != null)
                {
                    GameCommand reaction = Reaction(knowledge, statement.Actor, statement.Target ?? statement.Actor,
                        statement.Intent == SpeechIntent.Accuse || statement.Intent == SpeechIntent.Deflect, random);
                    if (reaction != null) candidates.Add((reaction, 18 + loyalty));
                }
            }
            EvidenceRecord last = knowledge.Evidence.LastOrDefault(e => e.Actor == knowledge.Self && e.Intent.HasValue);
            return candidates.Select(c => (c.command, score: c.utility + random.Next(9)
                - (last != null && last.Intent == c.command.Intent && last.Target == c.command.Target ? 24 : 0)))
                .OrderByDescending(c => c.score).First().command;
        }

        /// <inheritdoc />
        public CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random)
        {
            KnownCharacter suspect = SocialAssessment.Suspect(knowledge, random, nomination: true);
            int threshold = 38 + ((knowledge.Personality == Personality.Anxious || knowledge.Personality == Personality.Grim) ? _settings.CautiousThreshold : 0);
            return suspect != null && (knowledge.OwnRole.Faction == Faction.Antagonist
                || SocialAssessment.Threat(knowledge, suspect) >= threshold) ? suspect.Id : (CharacterId?)null;
        }

        /// <inheritdoc />
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random)
        {
            KnownCharacter target = knowledge.Characters.First(c => c.Id == defendant);
            if (target.IsTeammate || knowledge.OwnRole.Faction == Faction.Town && ConfirmedTown(target)) return Verdict.Innocent;
            int threshold = 50 + ((knowledge.Personality == Personality.Anxious || knowledge.Personality == Personality.Grim) ? _settings.CautiousThreshold : 0);
            return SocialAssessment.Judge(knowledge, target, threshold);
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.Charges == 0 || knowledge.OwnRole.Ability == AbilityKind.None) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.SelfProtect)
                return knowledge.SelfExposure >= _settings.ExposureThreshold || random.Next(100) < 45 ? knowledge.Self : (CharacterId?)null;
            var targets = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self && !c.IsTeammate).ToList();
            if (targets.Count == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
            {
                if (knowledge.OwnRole.Faction == Faction.Town)
                {
                    KnownCharacter suspect = targets.Where(c => !ConfirmedTown(c)).OrderByDescending(c => SocialAssessment.CaseStrength(knowledge, c))
                        .ThenByDescending(c => SocialAssessment.Threat(knowledge, c)).ThenBy(c => random.Next(1000)).FirstOrDefault();
                    return suspect != null && SocialAssessment.Threat(knowledge, suspect) >= 60 && SocialAssessment.CaseStrength(knowledge, suspect) >= 22 ? suspect.Id : (CharacterId?)null;
                }
                return targets.OrderByDescending(c => PublicThreat(knowledge, c)).ThenBy(c => random.Next(1000)).First().Id;
            }
            if (knowledge.OwnRole.Ability == AbilityKind.Protect)
                return targets.OrderByDescending(c => c.Trust + (100 - SocialAssessment.Threat(knowledge, c)) + (c.ClaimedRole.HasValue ? 20 : 0)).ThenBy(c => random.Next(1000)).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !knowledge.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == c.Id)).ToList();
                if (unknown.Count > 0) targets = unknown;
            }
            if (knowledge.OwnRole.Ability == AbilityKind.Block)
                return targets.OrderByDescending(c => PublicThreat(knowledge, c)).ThenBy(c => random.Next(1000)).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Investigate)
                return targets.OrderByDescending(c => (c.ClaimedRole.HasValue ? 25 : 0) + SocialAssessment.Threat(knowledge, c))
                    .ThenBy(c => random.Next(1000)).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Watch)
                return targets.OrderByDescending(c => PublicThreat(knowledge, c)).ThenBy(c => random.Next(1000)).First().Id;
            return targets.OrderByDescending(c => SocialAssessment.Threat(knowledge, c)).ThenBy(c => random.Next(1000)).First().Id;
        }

        internal static int PublicThreat(KnowledgeView knowledge, KnownCharacter character)
        {
            RoleSpec claim = character.ClaimedRole.HasValue ? knowledge.RoleCatalog.FirstOrDefault(r => r.Id == character.ClaimedRole.Value) : null;
            return character.Trust + (claim == null ? 0 : claim.Ability == AbilityKind.Investigate ? 35
                : claim.Ability == AbilityKind.Protect ? 25 : claim.Ability == AbilityKind.Attack ? 20 : 10);
        }

        private static bool HasReported(KnowledgeView knowledge, CharacterId target) => knowledge.Evidence.Any(e =>
            e.Actor == knowledge.Self && e.Kind == EvidenceKind.InvestigationClaim && e.Target == target && e.Night == knowledge.Day - 1);
        private static bool ConfirmedTown(KnownCharacter character) => character.KnownFaction == Faction.Town;
    }
}
