using SocialStim.Rules.Roles;
using UnityEngine;

namespace SocialStim.Data.Definitions
{
    /// <summary>Unity-authored role asset; converts to immutable rules data at run start.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Role Definition")]
    public sealed class RoleDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Faction _faction;
        [SerializeField] private RoleCategory _category;
        [SerializeField] private AbilityKind _ability;
        [SerializeField, Min(0)] private int _attack;
        [SerializeField, Min(0)] private int _defense;
        [SerializeField, Min(0)] private int _protection;
        [SerializeField] private int _charges = -1;
        [SerializeField] private Faction _appearance;
        [SerializeField] private bool _unique;

        [SerializeField] private bool _overrideClaimPermissions;
        [SerializeField] private bool _canClaimTruthfully = true;
        [SerializeField] private bool _canBluff;

        /// <summary>Creates validated runtime data without retaining this Unity object.</summary>
        public RoleSpec ToSpec() => new RoleSpec(new RoleId(_id), _displayName, _faction, _category,
            _ability, _attack, _defense, _protection, _charges, _appearance, _unique, _overrideClaimPermissions ? _canClaimTruthfully : (bool?)null,
            _overrideClaimPermissions ? _canBluff : (bool?)null);
    }
}
