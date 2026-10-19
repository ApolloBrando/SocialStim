using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>One candidate role's share of an observer's heuristic role distribution.</summary>
    public sealed class RoleEstimate
    {
        internal RoleEstimate(RoleId role, int basisPoints) { Role = role; BasisPoints = basisPoints; }
        /// <summary>Public catalog role being considered.</summary>
        public RoleId Role { get; }
        /// <summary>Nonnegative share; all candidate roles together sum to 10000.</summary>
        public int BasisPoints { get; }
    }
}
