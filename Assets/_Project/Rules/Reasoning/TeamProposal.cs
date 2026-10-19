using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>A faction-authorized kill proposal using an actor's own allowed knowledge.</summary>
    public sealed class TeamProposal
    {
        /// <summary>Creates one locked team preference.</summary>
        public TeamProposal(CharacterId actor, CharacterId? target, int priority = 0) { Actor = actor; Target = target; Priority = priority; }
        /// <summary>Team member able to carry out a kill.</summary>
        public CharacterId Actor { get; }
        /// <summary>Preferred target, or pass.</summary>
        public CharacterId? Target { get; }
        /// <summary>Bounded utility computed from permitted knowledge.</summary>
        public int Priority { get; }
    }
}
