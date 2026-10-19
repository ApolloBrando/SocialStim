using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Victory
{
    /// <summary>Parity policy valid only for the five-role catalog without independent lethal abilities.</summary>
    public sealed class StarterWinCondition : IWinCondition
    {
        /// <inheritdoc />
        public OutcomeKind Evaluate(IReadOnlyList<Faction> living, int day, int maximumDays)
        {
            int town = living.Count(f => f == Faction.Town);
            int antagonists = living.Count(f => f == Faction.Antagonist);
            if (town == 0 && antagonists == 0) return OutcomeKind.Draw;
            if (antagonists == 0) return OutcomeKind.TownVictory;
            if (antagonists >= town) return OutcomeKind.AntagonistVictory;
            return day >= maximumDays ? OutcomeKind.Draw : OutcomeKind.Ongoing;
        }
    }
}
