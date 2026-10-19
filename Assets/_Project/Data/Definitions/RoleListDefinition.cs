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
        [SerializeField, Min(1)] private int _townShare = 6;
        [SerializeField, Min(1)] private int _antagonistShare = 2;
        [SerializeField, Min(0)] private int _neutralShare = 1;

        /// <summary>Zero uses exact slots; a positive count uses the authored faction shares.</summary>
        public RoleList Load(int participants = 0) => participants == 0
            ? new RoleList(_slots?.Select(s => s?.Load()) ?? throw new InvalidOperationException("Role list slots missing."))
            : RoleList.ForPopulation(participants, _townShare, _antagonistShare, _neutralShare);
    }
}
