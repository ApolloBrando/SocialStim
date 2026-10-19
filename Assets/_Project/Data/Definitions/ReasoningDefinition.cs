using SocialStim.Rules.Reasoning;
using UnityEngine;

namespace SocialStim.Data.Definitions
{
    /// <summary>Inspector-authorable personality/evidence tuning, copied into immutable runtime settings.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Reasoning Definition")]
    public sealed class ReasoningDefinition : ScriptableObject
    {
        [SerializeField, Range(0, 100)] private int _assertionWeight = 9;
        [SerializeField, Range(0, 100)] private int _contradictionPenalty = 24;
        [SerializeField, Range(0, 100)] private int _aggressiveBonus = 18;
        [SerializeField, Range(0, 100)] private int _cautiousThreshold = 12;
        [SerializeField, Range(0, 100)] private int _gullibleBonus = 5;
        [SerializeField, Range(0, 100)] private int _loyaltyBonus = 8;
        [SerializeField, Range(0, 100)] private int _deceptionBonus = 20;
        [SerializeField, Range(0, 100)] private int _exposureThreshold = 45;

        /// <summary>Creates detached validated settings; editing an asset cannot mutate an active run.</summary>
        public ReasoningSettings ToSettings() => new ReasoningSettings(_assertionWeight, _contradictionPenalty,
            _aggressiveBonus, _cautiousThreshold, _gullibleBonus, _loyaltyBonus, _deceptionBonus, _exposureThreshold);
    }
}
