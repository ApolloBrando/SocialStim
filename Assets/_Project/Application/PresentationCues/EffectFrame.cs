namespace SocialStim.Application.PresentationCues
{
    /// <summary>Four scalar outputs; the Unity adapter owns colors and geometry.</summary>
    public readonly struct EffectFrame
    {
        public EffectFrame(float shake, float border, float vignette, float flash)
        { Shake = shake; Border = border; Vignette = vignette; Flash = flash; }
        public float Shake { get; }
        public float Border { get; }
        public float Vignette { get; }
        public float Flash { get; }
    }
}
