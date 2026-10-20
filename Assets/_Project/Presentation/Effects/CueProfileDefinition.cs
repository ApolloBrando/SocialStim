using System;
using System.Collections.Generic;
using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Inspector-authored overrides; converted once before playback.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Cue Profiles")]
    public sealed class CueProfileDefinition : ScriptableObject
    {
        [SerializeField] private CueProfileEntry[] _profiles = Array.Empty<CueProfileEntry>();
        public IReadOnlyDictionary<PresentationCue, EffectProfile> Load()
        {
            var result = new Dictionary<PresentationCue, EffectProfile>();
            foreach (var pair in DefaultCueProfiles.Create()) result.Add(pair.Key, pair.Value);
            var seen = new HashSet<PresentationCue>();
            foreach (CueProfileEntry entry in _profiles)
            {
                if (entry == null || !Enum.IsDefined(typeof(PresentationCue), entry.cue) || !seen.Add(entry.cue) || entry.tracks == null)
                    throw new ArgumentException("Invalid or duplicate cue profile.");
                var steps = new List<EffectStep>();
                foreach (EffectTrackDefinition track in entry.tracks)
                {
                    if (track == null) throw new ArgumentException("Missing effect track.");
                    steps.Add(track.ToStep());
                }
                result[entry.cue] = new EffectProfile(steps, entry.overrideTone ? entry.tone : result[entry.cue].Tone);
            }
            return result;
        }
    }
}
