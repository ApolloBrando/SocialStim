using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>One locked target selection; a missing target is a deliberate pass.</summary>
    public sealed class NightOrder
    {
        /// <summary>Creates a participant's selected action.</summary>
        public NightOrder(CharacterId actor, CharacterId? target) { Actor = actor; Target = target; }
        /// <summary>Acting participant.</summary>
        public CharacterId Actor { get; }
        /// <summary>Target, or null for a pass.</summary>
        public CharacterId? Target { get; }
    }
}
