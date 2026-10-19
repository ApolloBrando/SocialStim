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
            NightReport result = knowledge.PrivateResults.LastOrDefault(r => r.Kind == ReportKind.Investigation && r.Night == knowledge.Day - 1
                && knowledge.Characters.Any(c => c.Id == r.Target && c.Alive));
            if (result != null && random.Next(100) < 70)
                return GameCommand.Speak(SpeechIntent.ReportInvestigation, result.Target, faction: result.Alignment);
            if (knowledge.OwnRole.Faction == Faction.Antagonist && random.Next(100) < 25)
                return GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector"));
            KnownCharacter suspect = Suspect(knowledge);
            if (knowledge.OwnRole.Faction == Faction.Antagonist && knowledge.Day > 1 && suspect != null && random.Next(100) < 25)
                return GameCommand.Speak(SpeechIntent.ReportInvestigation, suspect.Id, faction: Faction.Antagonist);
            if (suspect != null && (suspect.Suspicion >= 30 || knowledge.Personality.HasFlag(Personality.Aggressive)))
                return GameCommand.Speak(SpeechIntent.Accuse, suspect.Id);
            int choice = random.Next(4);
            if (choice == 0) return GameCommand.Speak(SpeechIntent.ClaimRole,
                role: knowledge.OwnRole.Faction == Faction.Antagonist ? new RoleId("inspector") : knowledge.OwnRole.Id);
            if (choice == 1 && suspect != null) return GameCommand.Speak(SpeechIntent.Accuse, suspect.Id);
            return GameCommand.Speak(choice == 2 ? SpeechIntent.ExpressFear : SpeechIntent.StaySilent);
        }

        /// <inheritdoc />
        public CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random)
        {
            KnownCharacter suspect = Suspect(knowledge);
            int threshold = knowledge.Personality.HasFlag(Personality.Cautious) ? 34 : 24;
            return suspect != null && suspect.Suspicion >= threshold ? suspect.Id : (CharacterId?)null;
        }

        /// <inheritdoc />
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random)
        {
            KnownCharacter target = knowledge.Characters.First(c => c.Id == defendant);
            if (target.IsTeammate) return Verdict.Innocent;
            int threshold = knowledge.Personality.HasFlag(Personality.Cautious) ? 42 : 30;
            if (knowledge.Personality.HasFlag(Personality.Loyal)) threshold += 6;
            if (knowledge.OwnRole.Faction == Faction.Antagonist) threshold -= 12;
            return target.Suspicion >= threshold ? Verdict.Guilty : Verdict.Innocent;
        }

        /// <inheritdoc />
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random)
        {
            if (knowledge.Charges == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.SelfProtect)
                return random.Next(100) < 65 ? knowledge.Self : (CharacterId?)null;
            var targets = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self).ToList();
            if (knowledge.OwnRole.Ability == AbilityKind.Attack)
                targets = targets.Where(c => !c.IsTeammate).ToList();
            if (knowledge.OwnRole.Ability == AbilityKind.Investigate)
            {
                var unknown = targets.Where(c => !knowledge.PrivateResults.Any(r => r.Kind == ReportKind.Investigation && r.Target == c.Id)).ToList();
                if (unknown.Count > 0) targets = unknown;
            }
            if (targets.Count == 0) return null;
            if (knowledge.OwnRole.Ability == AbilityKind.Protect || knowledge.OwnRole.Ability == AbilityKind.Attack)
                return targets.OrderBy(c => c.Suspicion).ThenBy(c => c.Id.Value).First().Id;
            return targets.OrderByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).First().Id;
        }

        private static KnownCharacter Suspect(KnowledgeView knowledge) => knowledge.Characters
            .Where(c => c.Alive && c.Id != knowledge.Self && !c.IsTeammate)
            .OrderByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).FirstOrDefault();
    }
}
