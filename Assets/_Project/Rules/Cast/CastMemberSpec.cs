using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Explicit initial cast input for authored scenarios and resolver verification.</summary>
    public sealed class CastMemberSpec
    {
        /// <summary>Creates a scenario member; order defines the stable ID and slot zero is the player.</summary>
        public CastMemberSpec(string name, RoleId role, Personality personality = Personality.None)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A participant needs a name.", nameof(name));
            if (!Enum.IsDefined(typeof(Personality), personality))
                throw new ArgumentException("Choose exactly one registered trait.", nameof(personality));
            Name = name; Role = role; Personality = personality;
        }
        /// <summary>Public name.</summary>
        public string Name { get; }
        /// <summary>Initial authoritative role ID; not an observation.</summary>
        public RoleId Role { get; }
        /// <summary>Optional NPC trait; None requests seeded assignment. The player must use None.</summary>
        public Personality Personality { get; }
    }
}
