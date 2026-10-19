using System;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;
using UnityEngine;

namespace SocialStim.Data.Definitions
{
    /// <summary>Unity-authored role slots converted to detached constraints before a run.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Role List")]
    public sealed class RoleListDefinition : ScriptableObject
    {
        [Serializable]
        private sealed class Slot
        {
            [SerializeField] internal string Role;
            [SerializeField] internal bool FilterFaction;
            [SerializeField] internal Faction Faction;
            [SerializeField] internal bool FilterCategory;
            [SerializeField] internal RoleCategory Category;
            internal RoleSlot Load() => new RoleSlot(string.IsNullOrWhiteSpace(Role) ? (RoleId?)null : new RoleId(Role),
                FilterFaction ? Faction : (Faction?)null, FilterCategory ? Category : (RoleCategory?)null);
        }
        [SerializeField] private Slot[] _slots;
        public RoleList Load() => new RoleList(_slots?.Select(s => s?.Load()) ?? throw new InvalidOperationException("Role list slots missing."));
    }
}
