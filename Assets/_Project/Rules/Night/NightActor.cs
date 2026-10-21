using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Authoritative immutable night input; never a view/strategy payload.</summary>
    public sealed class NightActor
    {
        /// <summary>Captures one living actor's ability and remaining resources.</summary>
        public NightActor(CharacterId id, RoleSpec role, int charges, bool guiltDue = false)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            if (charges < -1) throw new ArgumentOutOfRangeException(nameof(charges));
            Id = id; Charges = charges; GuiltDue = guiltDue;
        }
        /// <summary>Unavoidable guilt resolves at the end of this night; the actor cannot select an ability.</summary>
        public bool GuiltDue { get; }
        /// <summary>Actor identity.</summary>
        public CharacterId Id { get; }
        /// <summary>Authoritative role definition.</summary>
        public RoleSpec Role { get; }
        /// <summary>Available charges, or -1 for unlimited.</summary>
        public int Charges { get; }
    }

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
