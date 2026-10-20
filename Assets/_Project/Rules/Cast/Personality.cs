namespace SocialStim.Rules.Cast
{
    /// <summary>One NPC voice/behavior trait. None belongs only to the player or an unassigned scenario slot.</summary>
    public enum Personality { None = 0, Anxious = 1, Brave = 2, Slow = 4, Chatterbox = 8, Smug = 16, Grim = 32, Jokester = 64 }
}
