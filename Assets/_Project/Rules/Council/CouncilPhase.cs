namespace SocialStim.Rules.Council
{
    /// <summary>Player-facing mission stages; reveal pacing belongs to presentation.</summary>
    public enum CouncilPhase {
        Discussion = 0,
        Vote = 1,
        Elimination = 2,
        Night = 4,
        Morning = 5,
        Ended = 6,
    }
}
