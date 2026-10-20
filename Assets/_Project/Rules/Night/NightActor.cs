using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Authoritative immutable night input; never a view/strategy payload.</summary>
    public sealed class NightActor
    {
        /// <summary>Captures one living actor's ability and remaining resources.</summary>
        public NightActor(CharacterId id, RoleSpec role, int charges, int guiltNight = 0)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            if (charges < -1 || guiltNight < 0) throw new ArgumentOutOfRangeException(nameof(charges));
            Id = id; Charges = charges; GuiltNight = guiltNight;
        }
        /// <summary>Actor identity.</summary>
        public CharacterId Id { get; }
        /// <summary>Authoritative role definition.</summary>
        public RoleSpec Role { get; }
        /// <summary>Available charges, or -1 for unlimited.</summary>
        public int Charges { get; }
        /// <summary>Scheduled unavoidable guilt death, or zero.</summary>
        public int GuiltNight { get; }
    }
}
