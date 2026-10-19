using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Authoritative actor state; never supplied to a strategy or view.</summary>
    internal sealed class Participant
    {
        internal Participant(CharacterId id, string name, RoleSpec role, Personality personality)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            Id = id; Name = name; Role = role; Personality = personality; Alive = true;
            Charges = role.Charges;
        }

        internal CharacterId Id { get; }
        internal string Name { get; }
        internal RoleSpec Role { get; }
        internal Personality Personality { get; }
        internal bool Alive { get; set; }
        internal int Charges { get; set; }
    }
}
