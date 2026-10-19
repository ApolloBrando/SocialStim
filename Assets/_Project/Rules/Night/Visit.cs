using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>Actual visit record, retained by authoritative resolution.</summary>
    public sealed class Visit
    {
        /// <summary>Records a completed visit.</summary>
        public Visit(CharacterId visitor, CharacterId target) { Visitor = visitor; Target = target; }
        /// <summary>Visitor identity.</summary>
        public CharacterId Visitor { get; }
        /// <summary>Visited identity.</summary>
        public CharacterId Target { get; }
    }
}
