using System;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Presentation.Effects
{
    [Serializable]
    public sealed class CueProfileEntry
    {
        public PresentationCue cue;
        public bool overrideTone;
        public EffectTone tone;
        public EffectTrackDefinition[] tracks = Array.Empty<EffectTrackDefinition>();
    }
}
