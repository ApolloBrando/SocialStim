using System.Collections.Generic;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>One observer's posterior, distinct from credibility and personal affinity.</summary>
    public sealed class CouncilBelief
    {
        internal CouncilBelief(IReadOnlyDictionary<RoleId, double> roles, double town, double evil, double neutral,
            double killer, double credibility, double affinity)
        { Roles = roles; Town = town; Evil = evil; Neutral = neutral; Killer = killer; Credibility = credibility; Affinity = affinity; }
        /// <summary>Normalized candidate-role probabilities.</summary>
        public IReadOnlyDictionary<RoleId, double> Roles { get; }
        /// <summary>Town marginal.</summary>
        public double Town { get; }
        /// <summary>Evil marginal.</summary>
        public double Evil { get; }
        /// <summary>Neutral marginal.</summary>
        public double Neutral { get; }
        /// <summary>Anarchy/last-survivor marginal.</summary>
        public double Killer { get; }
        /// <summary>Estimated reliability of assertions, between zero and one.</summary>
        public double Credibility { get; }
        /// <summary>Personal bond/grudge from minus one to one.</summary>
        public double Affinity { get; }
        /// <summary>Town's estimated hostile probability; passive neutrals are not automatically enemies.</summary>
        public double Hostile => Evil + Killer;
    }
}
