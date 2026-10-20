using System;
using System.Collections.Generic;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Small default vocabulary; scene profiles may override it without rule edits.</summary>
    public static class DefaultCueProfiles
    {
        public static IReadOnlyDictionary<PresentationCue, EffectProfile> Create()
        {
            var result = new Dictionary<PresentationCue, EffectProfile>();
            foreach (PresentationCue cue in Enum.GetValues(typeof(PresentationCue))) result.Add(cue, EffectProfile.None);
            result[PresentationCue.Dawn] = Pulse(EffectTone.Morning, 1.6f, 0.45f, 0.65f, 0.6f);
            result[PresentationCue.Dusk] = Pulse(EffectTone.Night, 1.6f, 0.5f, 0.7f, 0.65f, 0.35f);
            foreach (var cue in new[] { PresentationCue.Accusation, PresentationCue.Nomination, PresentationCue.Targeted })
                result[cue] = Pulse(EffectTone.Pressure, 0.9f, 0.8f, 0.65f, 0.3f, 0.3f, 0.25f);
            result[PresentationCue.Refutation] = Pulse(EffectTone.Pressure, 1.1f, 0.9f, 0.8f, 0.35f, 0.35f, 0.3f);
            foreach (var cue in new[] { PresentationCue.Defense, PresentationCue.Innocent, PresentationCue.Protection, PresentationCue.NarrowEscape })
                result[cue] = Pulse(EffectTone.Reassurance, 1.1f, 0.65f, 0.65f, 0.4f);
            result[PresentationCue.Agreement] = Pulse(EffectTone.Reassurance, 0.6f, 0.35f, 0.25f, 0);
            result[PresentationCue.Disagreement] = Pulse(EffectTone.Uncertainty, 0.7f, 0.5f, 0.4f, 0.15f);
            foreach (var cue in new[] { PresentationCue.Claim, PresentationCue.Reveal, PresentationCue.Investigation, PresentationCue.ActionReport })
                result[cue] = Pulse(EffectTone.Evidence, 1.1f, 0.7f, 0.65f, 0.35f);
            foreach (var cue in new[] { PresentationCue.CounterClaim, PresentationCue.ContradictedClaim, PresentationCue.Blocked })
                result[cue] = Pulse(EffectTone.Challenge, 1.2f, 0.85f, 0.8f, 0.4f, 0.35f, 0.25f);
            foreach (var cue in new[] { PresentationCue.Trial, PresentationCue.NearExposure, PresentationCue.Tally })
                result[cue] = Pulse(EffectTone.Judgement, 1.2f, 0.85f, 0.7f, 0.4f, 0.35f);
            result[PresentationCue.Ballot] = Pulse(EffectTone.Judgement, 0.55f, 0.35f, 0.2f, 0);
            foreach (var cue in new[] { PresentationCue.Tie, PresentationCue.Draw, PresentationCue.ResourcesDepleted, PresentationCue.FinalThree, PresentationCue.Fear })
                result[cue] = Pulse(EffectTone.Uncertainty, 1.2f, 0.6f, 0.6f, 0.3f, 0.35f);
            result[PresentationCue.Guilty] = Pulse(EffectTone.Loss, 1.4f, 0.9f, 0.9f, 0.5f, 0.65f);
            foreach (var cue in new[] { PresentationCue.Execution, PresentationCue.Death, PresentationCue.PlayerKilled, PresentationCue.Defeat })
                result[cue] = Pulse(EffectTone.Loss, 1.6f, 0.85f, 0.75f, 0.6f, 0.8f, 0.3f, 0.4f);
            foreach (var cue in new[] { PresentationCue.TeammateLost, PresentationCue.LastWords })
                result[cue] = Pulse(EffectTone.Loss, 1.4f, 0.55f, 0.5f, 0.3f, 0.55f);
            result[PresentationCue.Victory] = Pulse(EffectTone.Triumph, 1.8f, 0.85f, 0.9f, 0.65f);
            return result;
        }

        private static EffectProfile Pulse(EffectTone tone, float duration, float border, float glow, float wash,
            float vignette = 0, float shake = 0, float flash = 0)
        {
            var steps = new List<EffectStep> {
                new EffectStep(EffectChannel.Border, duration, border),
                new EffectStep(EffectChannel.EdgeGlow, duration, glow),
                new EffectStep(EffectChannel.ColorWash, duration, wash),
                new EffectStep(EffectChannel.Vignette, duration, vignette),
                new EffectStep(EffectChannel.TextShake, Math.Min(1.5f, duration), shake)
            };
            if (flash > 0) steps.Add(new EffectStep(EffectChannel.Flash, Math.Max(1, duration), flash));
            return new EffectProfile(steps, tone);
        }
    }
}
