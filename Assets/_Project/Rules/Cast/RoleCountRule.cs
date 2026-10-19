using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Per-run role availability and multiplicity; maximum zero disables a definition.</summary>
    public sealed class RoleCountRule
    {
        public RoleCountRule(RoleId role, int minimum = 0, int maximum = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(role.Value) || minimum < 0 || maximum < minimum)
                throw new ArgumentException("Invalid role minimum/maximum.");
            Role = role; Minimum = minimum; Maximum = maximum;
        }
        public RoleId Role { get; }
        public int Minimum { get; }
        public int Maximum { get; }
    }
}
