using System;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Validated integer tuning shared by evidence and social utility strategies.</summary>
    public sealed class ReasoningSettings
    {
        /// <summary>Creates tuning; weights are bounded to keep deterministic integer scoring safe.</summary>
        public ReasoningSettings(int assertionWeight = 9, int contradictionPenalty = 24,
            int aggressiveBonus = 18, int cautiousThreshold = 12, int gullibleBonus = 5,
            int loyaltyBonus = 8, int deceptionBonus = 20, int exposureThreshold = 45)
        {
            foreach (int value in new[] { assertionWeight, contradictionPenalty, aggressiveBonus,
                cautiousThreshold, gullibleBonus, loyaltyBonus, deceptionBonus, exposureThreshold })
                if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(assertionWeight));
            AssertionWeight = assertionWeight; ContradictionPenalty = contradictionPenalty;
            AggressiveBonus = aggressiveBonus; CautiousThreshold = cautiousThreshold;
            GullibleBonus = gullibleBonus; LoyaltyBonus = loyaltyBonus;
            DeceptionBonus = deceptionBonus; ExposureThreshold = exposureThreshold;
        }
        /// <summary>Base influence of an uncertain assertion.</summary>
        public int AssertionWeight { get; }
        /// <summary>Suspicion/trust penalty for a direct self-contradiction.</summary>
        public int ContradictionPenalty { get; }
        /// <summary>Accusation utility added by aggression.</summary>
        public int AggressiveBonus { get; }
        /// <summary>Extra evidence threshold for cautious decisions.</summary>
        public int CautiousThreshold { get; }
        /// <summary>Extra assertion influence on gullible observers.</summary>
        public int GullibleBonus { get; }
        /// <summary>Additional reluctance to condemn trusted people.</summary>
        public int LoyaltyBonus { get; }
        /// <summary>Utility bias toward consistent deception.</summary>
        public int DeceptionBonus { get; }
        /// <summary>Visible pressure at which self-defense becomes attractive.</summary>
        public int ExposureThreshold { get; }
    }
}
