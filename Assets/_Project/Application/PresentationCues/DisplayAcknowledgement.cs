namespace SocialStim.Application.PresentationCues
{
    /// <summary>Display-only result; cannot issue gameplay commands.</summary>
    public readonly struct DisplayAcknowledgement
    {
        public DisplayAcknowledgement(int sequence, DisplayCompletion completion) { Sequence = sequence; Completion = completion; }
        public int Sequence { get; }
        public DisplayCompletion Completion { get; }
    }
}
