namespace SocialStim.Application.PresentationCues
{
    /// <summary>Allocation-free glyph transform and appearance result.</summary>
    public readonly struct GlyphTransform
    {
        /// <summary>Creates a transform; positive scales and alpha are enforced by the sampler.</summary>
        public GlyphTransform(float x = 0, float y = 0, float sx = 1, float sy = 1, float angle = 0, float alpha = 1, float color = 0)
        { X = x; Y = y; ScaleX = sx; ScaleY = sy; Angle = angle; Alpha = alpha; Color = color; }
        /// <summary>Horizontal displacement.</summary>
        public float X { get; }
        /// <summary>Vertical displacement.</summary>
        public float Y { get; }
        /// <summary>Horizontal scale.</summary>
        public float ScaleX { get; }
        /// <summary>Vertical scale.</summary>
        public float ScaleY { get; }
        /// <summary>Rotation in radians.</summary>
        public float Angle { get; }
        /// <summary>Opacity multiplier.</summary>
        public float Alpha { get; }
        /// <summary>Blend towards the contrast-preserving evidence accent.</summary>
        public float Color { get; }
        /// <summary>Explicit identity; default(struct) is intentionally not an identity transform.</summary>
        public static GlyphTransform Identity => new GlyphTransform(sx: 1, sy: 1, alpha: 1);
    }
}
