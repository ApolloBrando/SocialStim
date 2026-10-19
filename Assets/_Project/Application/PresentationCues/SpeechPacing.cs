using SocialStim.Rules.Phases;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Stable presentation-only speaker tempo; never consumes a rules random stream.</summary>
    public readonly struct SpeechPacing
    {
        public SpeechPacing(GameObservation observation)
        {
            uint speaker = unchecked((uint)(observation.Speaker?.Value ?? 17) + 1u);
            uint variation = unchecked((uint)observation.Sequence * 2654435761u + speaker * 2246822519u);
            Speed = observation.Speaker.HasValue ? 0.78f + (speaker * 37u % 45u) / 100f : 1.25f;
            Pause = observation.Cue == GameCue.SpeechDelivered ? 0.2f + variation % 35u / 100f : 0.1f;
        }
        public float Speed { get; }
        public float Pause { get; }
    }
}
