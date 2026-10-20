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
            RoleSpec decoy = ClaimPolicy.Proactive(knowledge);
            RoleId claim = decoy?.Id ?? knowledge.OwnRole.Id;
            NightReport result = knowledge.PrivateResults.LastOrDefault(r => r.Kind == ReportKind.Investigation && r.Night == knowledge.Day - 1
                && knowledge.Characters.Any(c => c.Id == r.Target && c.Alive));
            if (result != null && random.Next(100) < 70)
                return GameCommand.Speak(SpeechIntent.ReportInvestigation, result.Target, faction: result.Alignment);
            if (knowledge.OwnRole.CanBluff && decoy != null && random.Next(100) < 25)
                return GameCommand.Speak(SpeechIntent.ClaimRole, role: claim);
            KnownCharacter suspect = Suspect(knowledge);
            if (knowledge.OwnRole.Faction == Faction.Antagonist && knowledge.Day > 1 && suspect != null && random.Next(100) < 25)
                return GameCommand.Speak(SpeechIntent.ReportInvestigation, suspect.Id, faction: Faction.Antagonist);
            if (suspect != null && (suspect.Suspicion >= 30 || (knowledge.Personality == Personality.Brave || knowledge.Personality == Personality.Smug)))
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
            KnownCharacter suspect = Suspect(knowledge);
            int threshold = (knowledge.Personality == Personality.Anxious || knowledge.Personality == Personality.Grim) ? 34 : 24;
            return suspect != null && suspect.Suspicion >= threshold ? suspect.Id : (CharacterId?)null;
        }

        /// <inheritdoc />
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random)
        {
            KnownCharacter target = knowledge.Characters.First(c => c.Id == defendant);
            if (target.IsTeammate || knowledge.OwnRole.Faction == Faction.Town && ConfirmedTown(target)) return Verdict.Innocent;
            int threshold = (knowledge.Personality == Personality.Anxious || knowledge.Personality == Personality.Grim) ? 42 : 30;
            if ((knowledge.Personality == Personality.Brave)) threshold += 6;
            if (knowledge.OwnRole.Faction == Faction.Antagonist) threshold -= 12;
            return target.Suspicion >= threshold ? Verdict.Guilty : Verdict.Innocent;
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.Charges == 0 || knowledge.OwnRole.Ability == AbilityKind.None) return null;
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
                return targets.OrderBy(c => c.Suspicion).ThenBy(c => c.Id.Value).First().Id;
            if (knowledge.OwnRole.Ability == AbilityKind.Attack && knowledge.OwnRole.Faction == Faction.Town)
            {
                KnownCharacter suspect = targets.OrderByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).First();
                return suspect.Suspicion >= 60 ? suspect.Id : (CharacterId?)null;
            }
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
                return targets.OrderBy(c => c.Suspicion).ThenBy(c => c.Id.Value).First().Id;
            return targets.OrderByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).First().Id;
        }

        private static bool ConfirmedTown(KnownCharacter character) => character.KnownFaction == Faction.Town;
        private static KnownCharacter Suspect(KnowledgeView knowledge) => knowledge.Characters
            .Where(c => c.Alive && c.Id != knowledge.Self && !c.IsTeammate
                && (knowledge.OwnRole.Faction != Faction.Town || !ConfirmedTown(c)))
            .OrderByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).FirstOrDefault();
    }
}
