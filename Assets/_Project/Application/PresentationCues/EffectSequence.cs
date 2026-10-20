using System;
using System.Collections.Generic;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Authoring-time sequence/parallel composition; never used in steady updates.</summary>
    public sealed class EffectSequence
    {
        private readonly List<EffectStep> _steps = new List<EffectStep>();
        private float _cursor;
        public EffectSequence Parallel(params EffectStep[] steps)
        {
            float end = _cursor;
            foreach (EffectStep step in steps)
            {
                _steps.Add(new EffectStep(step.Channel, step.Duration, step.Amount, step.Priority, step.Blend, _cursor + step.Delay, step.Envelope, step.Continuous));
                end = Math.Max(end, _cursor + step.Delay + step.Duration);
            }
            _cursor = end;
            return this;
        }
        public EffectSequence Then(EffectStep step) => Parallel(step);
        public EffectProfile Build(EffectTone tone = EffectTone.Neutral) => new EffectProfile(_steps, tone);
    }
}
