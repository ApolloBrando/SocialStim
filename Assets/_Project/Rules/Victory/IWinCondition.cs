using System.Collections.Generic;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Victory
{
    /// <summary>Pluggable stable-boundary faction outcome evaluation.</summary>
    public interface IWinCondition
    {
        /// <summary>Evaluates the living alignments and configured day cap.</summary>
        OutcomeKind Evaluate(IReadOnlyList<Faction> living, int day, int maximumDays);
    }
}
