using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Private shared faction plan, never a public cast observation.</summary>
    public sealed class TeamNightPlan
    {
        internal TeamNightPlan(CharacterId killer, CharacterId? target) { Killer = killer; Target = target; }
        /// <summary>Designated acting teammate.</summary>
        public CharacterId Killer { get; }
        /// <summary>Agreed target, or no kill.</summary>
        public CharacterId? Target { get; }
    }
}
