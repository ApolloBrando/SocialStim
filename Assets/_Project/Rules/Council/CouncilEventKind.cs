namespace SocialStim.Rules.Council
{
    /// <summary>Meaning/provenance of a record, independent of displayed prose and effects.</summary>
    public enum CouncilEventKind {
        Phase = 0,
        Speech = 1,
        Claim = 2,
        Report = 3,
        Vote = 4,
        Tally = 5,
        Tie = 6,
        Elimination = 7,
        RoleReveal = 8,
        Death = 9,
        Morning = 10,
        PrivateResult = 11,
        Promotion = 12,
        GameOver = 13,
        NightOrder = 14,
        Decision = 16,
    }
}
