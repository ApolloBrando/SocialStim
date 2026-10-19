using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Explicit initial cast input for authored scenarios and resolver verification.</summary>
    public sealed class CastMemberSpec
    {
        /// <summary>Creates a scenario member; order defines the stable ID and slot zero is the player.</summary>
        public CastMemberSpec(string name, RoleId role, Personality personality = Personality.None)
        { Name = name; Role = role; Personality = personality; }
        /// <summary>Public name.</summary>
        public string Name { get; }
        /// <summary>Initial authoritative role ID; not an observation.</summary>
        public RoleId Role { get; }
        /// <summary>Initial tendencies.</summary>
        public Personality Personality { get; }
    }
}
