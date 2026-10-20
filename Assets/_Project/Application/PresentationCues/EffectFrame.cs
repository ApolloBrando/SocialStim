namespace SocialStim.Application.PresentationCues
{
    /// <summary>Bounded scalar outputs; the Unity adapter owns colours and geometry.</summary>
    public readonly struct EffectFrame
    {
        public EffectFrame(float shake, float border, float vignette, float flash, float colorWash = 0, float edgeGlow = 0)
        { Shake = shake; Border = border; Vignette = vignette; Flash = flash; ColorWash = colorWash; EdgeGlow = edgeGlow; }
        public float Shake { get; }
        public float Border { get; }
        public float Vignette { get; }
        public float Flash { get; }
        /// <summary>Low-opacity situation tint, independent of optional flashes.</summary>
        public float ColorWash { get; }
        /// <summary>Soft peripheral accent, suppressed by reduced motion.</summary>
        public float EdgeGlow { get; }
    }
}
