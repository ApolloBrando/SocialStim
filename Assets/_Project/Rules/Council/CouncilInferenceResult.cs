using System.Collections.Generic;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Council
{
    /// <summary>Immutable diagnostic and decision result from bounded observer-scoped inference.</summary>
    public sealed class CouncilInferenceResult
    {
        internal CouncilInferenceResult(IReadOnlyDictionary<CharacterId, CouncilBelief> beliefs, int samples, double effectiveSamples, double exposure, double tension)
        { Beliefs = beliefs; Samples = samples; EffectiveSamples = effectiveSamples; Exposure = exposure; Tension = tension; }
        /// <summary>Observer-local distributions, never authoritative foreign roles.</summary>
        public IReadOnlyDictionary<CharacterId, CouncilBelief> Beliefs { get; }
        /// <summary>Valid assignments evaluated.</summary>
        public int Samples { get; }
        /// <summary>Importance-weight effective sample count, useful for diagnosing overconfidence.</summary>
        public double EffectiveSamples { get; }
        /// <summary>Estimated public pressure against the observer.</summary>
        public double Exposure { get; }
        /// <summary>Public death/day-driven paranoia, from zero to one.</summary>
        public double Tension { get; }
    }
}
