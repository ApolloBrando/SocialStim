using System;
using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    [Serializable]
    public sealed class EffectTrackDefinition
    {
        public EffectChannel channel;
        [Range(0.05f, 10)] public float duration = 0.8f;
        [Range(0, 1)] public float amount = 0.5f;
        [Range(0, 10)] public float delay;
        public int priority;
        public EffectBlend blend;
        public EffectEnvelope envelope;
        [SerializeField] private bool _continuous;
        public EffectStep ToStep() => new EffectStep(channel, duration, amount, priority, blend, delay, envelope, _continuous);
    }
}
