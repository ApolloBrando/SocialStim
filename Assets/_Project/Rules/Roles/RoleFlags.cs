using System;

namespace SocialStim.Rules.Roles
{
    /// <summary>Reusable role capabilities; role identity never drives core behavior.</summary>
    [Flags]
    public enum RoleFlags { None = 0, NightImmune = 1, ReadsClear = 2, SelfHeal = 4, Backlash = 8, Promotable = 16 }
}
