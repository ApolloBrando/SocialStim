using System;

namespace SocialStim.Rules.Cast
{
    /// <summary>Independent behavior tendencies; flags may coexist.</summary>
    [Flags]
    public enum Personality { None = 0, Aggressive = 1, Cautious = 2, Gullible = 4, Loyal = 8, Deceptive = 16 }
}
