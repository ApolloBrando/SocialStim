namespace SocialStim.Application.PresentationCues
{
    /// <summary>A UTF-16 range in the parsed plain text.</summary>
    public readonly struct SemanticSpan
    {
        public SemanticSpan(int start, int length, SemanticStyle style) { Start = start; Length = length; Style = style; }
        public int Start { get; }
        public int Length { get; }
        public SemanticStyle Style { get; }
    }
}
