using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Inspectable initial utility rules; no access to authoritative cast state.</summary>
    public sealed class BasicNpcStrategy : INpcStrategy
    {
        /// <inheritdoc />
        public GameCommand Speak(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.CanGiveReport) return GameCommand.ReportLastAction();
            RoleSpec decoy = ClaimPolicy.Proactive(knowledge);
            RoleId claim = decoy?.Id ?? knowledge.OwnRole.Id;
            NightReport result = knowledge.PrivateResults.LastOrDefault(r => r.Kind == ReportKind.Investigation && r.Night == knowledge.Day - 1
                && knowledge.Characters.Any(c => c.Id == r.Target && c.Alive));
            if (result != null && knowledge.LastNightAction == null && random.Next(100) < 70)
                return GameCommand.Speak(SpeechIntent.ReportInvestigation, result.Target, faction: result.Alignment);
            if (knowledge.OwnRole.CanBluff && decoy != null && random.Next(100) < 25)
                return GameCommand.Speak(SpeechIntent.ClaimRole, role: claim);
            KnownCharacter suspect = SocialAssessment.Suspect(knowledge, random);
            GameCommand fabrication = ClaimPolicy.FabricatedInvestigation(knowledge, random);
            if (fabrication != null && knowledge.LastNightAction == null && random.Next(100) < 25) return fabrication;
            if (suspect != null && (SocialAssessment.Threat(knowledge, suspect) >= 45))
                return GameCommand.Speak(SpeechIntent.Accuse, suspect.Id);
            int choice = random.Next(4);
            if (choice == 0 && decoy != null && (knowledge.OwnRole.CanBluff || knowledge.SelfExposure >= 40))
                return GameCommand.Speak(SpeechIntent.ClaimRole, role: decoy.Id);
            if (choice == 1 && suspect != null) return GameCommand.Speak(SpeechIntent.Accuse, suspect.Id);
            return GameCommand.Speak(choice == 2 ? SpeechIntent.ExpressFear : SpeechIntent.StaySilent);
        }

        /// <inheritdoc />
        public CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random)
        {
            KnownCharacter suspect = SocialAssessment.Suspect(knowledge, random, nomination: true);
            int threshold = (knowledge.Personality == Personality.Anxious || knowledge.Personality == Personality.Grim) ? 34 : 24;
            return suspect != null && (knowledge.OwnRole.Faction == Faction.Antagonist
                || SocialAssessment.Threat(knowledge, suspect) >= threshold) ? suspect.Id : (CharacterId?)null;
        }

        /// <inheritdoc />
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random)
        {
            KnownCharacter target = knowledge.Characters.First(c => c.Id == defendant);
            if (target.IsTeammate || knowledge.OwnRole.Faction == Faction.Town && ConfirmedTown(target)) return Verdict.Innocent;
            return SocialAssessment.Judge(knowledge, target, 50);
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.Day < knowledge.OwnRole.FirstActionNight || knowledge.Charges == 0 || knowledge.OwnRole.Ability == AbilityKind.None) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.SelfProtect)
                return random.Next(100) < 65 ? knowledge.Self : (CharacterId?)null;
            var targets = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self).ToList();
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
                targets = targets.Where(c => !c.IsTeammate && (knowledge.OwnRole.Faction != Faction.Town || !ConfirmedTown(c))).ToList();
            if (knowledge.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !knowledge.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == c.Id)).ToList();
                if (unknown.Count > 0) targets = unknown;
            }
            if (targets.Count == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.Protect)
                return targets.OrderBy(c => c.Suspicion).ThenBy(c => random.Next(1000)).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Attack && knowledge.OwnRole.Faction == Faction.Town)
            {
                KnownCharacter suspect = targets.OrderByDescending(c => SocialAssessment.CaseStrength(knowledge, c))
                    .ThenByDescending(c => c.Suspicion).ThenBy(c => random.Next(1000)).First();
                return SocialAssessment.Threat(knowledge, suspect) >= 60 && SocialAssessment.CaseStrength(knowledge, suspect) >= 22 ? suspect.Id : (CharacterId?)null;
            }
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
                return targets.OrderBy(c => c.Suspicion).ThenBy(c => random.Next(1000)).First().Id;
            return targets.OrderByDescending(c => c.Suspicion).ThenBy(c => random.Next(1000)).First().Id;
        }

        private static bool ConfirmedTown(KnownCharacter character) => character.KnownFaction == Faction.Town;
    }
}
