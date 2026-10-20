namespace SocialStim.Application.PresentationCues
{
    /// <summary>A glyph effect applied to a parsed UTF-16 range.</summary>
    public readonly struct GlyphEffectSpan
    {
        /// <summary>Captures an immutable effect and its text range.</summary>
        public GlyphEffectSpan(int start, int length, GlyphEffect effect) { Start = start; Length = length; Effect = effect; }
        /// <summary>Inclusive UTF-16 start.</summary>
        public int Start { get; }
        /// <summary>UTF-16 range length.</summary>
        public int Length { get; }
        /// <summary>Validated effect recipe.</summary>
        public GlyphEffect Effect { get; }
    }
}
