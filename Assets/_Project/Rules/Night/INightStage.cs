namespace SocialStim.Rules.Night
{
    /// <summary>One ordered rule stage; ordering is supplied at composition time.</summary>
    public interface INightStage
    {
        /// <summary>Stable stage priority; duplicate priorities are rejected.</summary>
        int Priority { get; }
        /// <summary>Resolves against authoritative night context.</summary>
        void Resolve(NightContext context);
    }
}
