namespace SocialStim.Rules.Council
{
    /// <summary>Portable semantic commands; menu navigation never appears here.</summary>
    public enum CouncilActionKind {
        Chat = 0,
        Accuse = 1,
        Defend = 2,
        Claim = 3,
        Report = 4,
        Pass = 5,
        Vote = 6,
        Night = 7,
        Continue = 8,
    }
}
