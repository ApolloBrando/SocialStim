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
            var gentle = new EffectSequence().Parallel(new EffectStep(EffectChannel.Border, 0.65f, 0.4f)).Build();
            var tense = new EffectSequence().Parallel(new EffectStep(EffectChannel.Border, 0.8f, 0.7f),
                new EffectStep(EffectChannel.TextShake, 1.5f, 0.4f), new EffectStep(EffectChannel.Vignette, 0.8f, 0.45f)).Build();
            var decisive = new EffectSequence().Parallel(new EffectStep(EffectChannel.Border, 1.2f, 0.9f),
                new EffectStep(EffectChannel.Vignette, 1.2f, 0.7f), new EffectStep(EffectChannel.Flash, 1.2f, 0.5f)).Build();
            foreach (PresentationCue cue in new[] { PresentationCue.Dawn, PresentationCue.Dusk, PresentationCue.Nomination,
                PresentationCue.Defense, PresentationCue.Innocent, PresentationCue.Reveal, PresentationCue.Investigation, PresentationCue.Tie,
                PresentationCue.Victory, PresentationCue.Draw, PresentationCue.ResourcesDepleted }) result[cue] = gentle;
            foreach (PresentationCue cue in new[] { PresentationCue.Accusation, PresentationCue.Trial, PresentationCue.Targeted,
                PresentationCue.Blocked, PresentationCue.NearExposure, PresentationCue.NarrowEscape, PresentationCue.ContradictedClaim,
                PresentationCue.TeammateLost, PresentationCue.FinalThree }) result[cue] = tense;
            foreach (PresentationCue cue in new[] { PresentationCue.Guilty, PresentationCue.Execution, PresentationCue.Death,
                PresentationCue.PlayerKilled, PresentationCue.Defeat }) result[cue] = decisive;
            return result;
        }
    }
}
