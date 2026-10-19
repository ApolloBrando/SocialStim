using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Authoritative state, kept internal to the rules assembly.</summary>
    internal sealed class CouncilActor
    {
        internal CouncilActor(CharacterId id, CastMemberSpec spec, RoleSpec role)
        { Id = id; Name = spec.Name; Traits = spec.Personality; StartingRole = role; Role = role; Charges = role.Charges; }
        internal CharacterId Id { get; }
        internal string Name { get; }
        internal Personality Traits { get; }
        internal RoleSpec StartingRole { get; }
        internal RoleSpec Role { get; set; }
        internal int Charges { get; set; }
        internal bool Alive { get; set; } = true;
        internal bool SelfHealUsed { get; set; }
        internal bool BacklashDue { get; set; }
        internal bool VotedOut { get; set; }
        internal List<NightReport> Results { get; } = new List<NightReport>();
        internal NightActor Night() => new NightActor(Id, Role, Charges, SelfHealUsed, BacklashDue);
    }
}
