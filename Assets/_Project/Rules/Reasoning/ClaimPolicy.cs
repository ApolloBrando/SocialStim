using System.Linq;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Shared scoped claim choices; a claim conflict is handled by the immediate response window.</summary>
    public static class ClaimPolicy
    {
        /// <summary>Chooses an unclaimed legal role without access to hidden assignments.</summary>
        public static RoleSpec Proactive(KnowledgeView knowledge)
        {
            if (knowledge.Characters.First(c => c.Id == knowledge.Self).ClaimedRole.HasValue) return null;
            return knowledge.RoleCatalog.Where(role => knowledge.OwnRole.CanClaim(role)
                    && !knowledge.Characters.Any(c => c.Alive && c.ClaimedRole == role.Id))
                .OrderByDescending(role => Plausibility(knowledge, role))
                .ThenBy(role => role.Id.Value, System.StringComparer.Ordinal).FirstOrDefault();
        }

        private static int Plausibility(KnowledgeView knowledge, RoleSpec role)
        {
            // Match the kind of visit the liar can actually make; vary cover preferences by voice.
            // This uses the public catalog and own ability, never the hidden setup assignment.
            int score = role.Ability == knowledge.OwnRole.Ability ? 30 : 0;
            if (knowledge.OwnRole.CanBluff)
            {
                if ((int)knowledge.Personality % 3 == 0 && role.Ability == AbilityKind.Investigate) score += 25;
                if ((int)knowledge.Personality % 3 == 1 && role.Ability == AbilityKind.Protect) score += 25;
                if ((int)knowledge.Personality % 3 == 2 && role.Ability == AbilityKind.Watch) score += 25;
            }
            return score;
        }

        /// <summary>One mechanically plausible investigation story per night, only after publicly claiming that ability.</summary>
        public static GameCommand FabricatedInvestigation(KnowledgeView knowledge, IRandomSource random)
        {
            var self = knowledge.Characters.First(c => c.Id == knowledge.Self);
            if (!knowledge.OwnRole.CanBluff || knowledge.Day <= 1 || !self.ClaimedRole.HasValue
                || !knowledge.RoleCatalog.Any(r => r.Id == self.ClaimedRole && r.Ability == AbilityKind.Investigate)
                || knowledge.Evidence.Any(e => e.Actor == knowledge.Self && e.Kind == EvidenceKind.InvestigationClaim && e.Night == knowledge.Day - 1)) return null;
            var targets = knowledge.Characters.Where(c => c.Alive && c.Id != knowledge.Self).ToList();
            if (targets.Count == 0) return null;
            bool frame = random.Next(100) < 35;
            var eligible = targets.Where(c => !frame || !c.IsTeammate).ToList();
            if (eligible.Count == 0) { eligible = targets; frame = false; }
            var target = eligible.OrderByDescending(c => frame ? SocialNpcStrategy.PublicThreat(knowledge, c)
                : c.IsTeammate ? 120 : c.Trust).ThenBy(c => random.Next(1000)).First();
            return GameCommand.Speak(SpeechIntent.ReportInvestigation, target.Id, faction: frame ? Faction.Antagonist : Faction.Town);
        }
    }
}
