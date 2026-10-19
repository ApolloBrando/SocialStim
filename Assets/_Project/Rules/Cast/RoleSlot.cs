using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Authored slot constraints are combined; null constraints accept any role.</summary>
    public sealed class RoleSlot
    {
        public RoleSlot(RoleId? role = null, Faction? faction = null, RoleCategory? category = null)
        { Role = role; Faction = faction; Category = category; }
        public RoleId? Role { get; }
        public Faction? Faction { get; }
        public RoleCategory? Category { get; }
        public bool Accepts(RoleSpec role) => (!Role.HasValue || Role.Value == role.Id)
            && (!Faction.HasValue || Faction.Value == role.Faction) && (!Category.HasValue || Category.Value == role.Category);
    }
}
