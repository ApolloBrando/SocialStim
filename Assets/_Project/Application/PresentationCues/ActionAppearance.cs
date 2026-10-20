using SocialStim.Rules.Dialogue;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Public action colours describe intent; they never certify the speaker's faction or truth.</summary>
    public static class ActionAppearance
    {
        public static EffectTone Tone(SpeechIntent? intent)
        {
            switch (intent)
            {
                case SpeechIntent.Accuse: case SpeechIntent.CallForTrial: case SpeechIntent.Disagree: return EffectTone.Pressure;
                case SpeechIntent.Defend: case SpeechIntent.Vouch: case SpeechIntent.Agree: return EffectTone.Reassurance;
                case SpeechIntent.CounterClaim: case SpeechIntent.Deflect: return EffectTone.Challenge;
                case SpeechIntent.ClaimRole: case SpeechIntent.ReportAction: case SpeechIntent.ReportInvestigation: return EffectTone.Evidence;
                default: return EffectTone.Neutral;
            }
        }
        public static SemanticText Label(SemanticText line, SpeechIntent? intent, GameText words)
        {
            if (!intent.HasValue || !words.Entries.TryGetValue("intent." + intent.Value, out string label)) return line;
            string prefix = "[" + label + "] ";
            var spans = new System.Collections.Generic.List<SemanticSpan>();
            var tone = Tone(intent);
            var style = tone == EffectTone.Pressure ? SemanticStyle.ActionPressure
                : tone == EffectTone.Reassurance ? SemanticStyle.ActionSupport
                : tone == EffectTone.Challenge ? SemanticStyle.ActionChallenge : SemanticStyle.ActionEvidence;
            spans.Add(new SemanticSpan(0, prefix.Length, style));
            foreach (var span in line.Spans) spans.Add(new SemanticSpan(prefix.Length + span.Start, span.Length, span.Style));
            return new SemanticText(prefix + line.Text, spans);
        }
    }
}
