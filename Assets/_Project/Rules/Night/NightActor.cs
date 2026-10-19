using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Authoritative immutable night input; never a view/strategy payload.</summary>
    public sealed class NightActor
    {
        /// <summary>Captures one living actor's ability and remaining resources.</summary>
        public NightActor(CharacterId id, RoleSpec role, int charges, bool selfHealUsed = false, bool backlashDue = false)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            if (charges < -1) throw new ArgumentOutOfRangeException(nameof(charges));
            Id = id; Charges = charges; SelfHealUsed = selfHealUsed; BacklashDue = backlashDue;
        }
        /// <summary>Actor identity.</summary>
        public CharacterId Id { get; }
        /// <summary>Authoritative role definition.</summary>
        public RoleSpec Role { get; }
        /// <summary>Available charges, or -1 for unlimited.</summary>
        public int Charges { get; }
        /// <summary>Whether the once-per-run self-heal has been spent.</summary>
        public bool SelfHealUsed { get; }
        /// <summary>Previously scheduled backlash resolves in this batch cleanup.</summary>
        public bool BacklashDue { get; }
    }
}
