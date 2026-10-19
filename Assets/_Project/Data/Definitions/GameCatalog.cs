using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Reasoning;
using UnityEngine;

namespace SocialStim.Data.Definitions
{
    /// <summary>Explicit content references for an initial self-contained build.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [SerializeField] private RoleDefinition[] _roles;
        [SerializeField] private RoleDefinition[] _missionRoles;
        [SerializeField] private TextAsset _dialogue;
        [SerializeField] private string[] _names;
        [SerializeField] private ReasoningDefinition _reasoning;
        [SerializeField] private RoleListDefinition _setup;
        public RoleList LoadSetup(int participants = 0) => _setup != null ? _setup.Load(participants)
            : participants > 0 ? RoleList.ForPopulation(participants) : null;

        /// <summary>Loads detached role definitions, rejecting missing/duplicate assets.</summary>
        public IReadOnlyList<RoleSpec> LoadRoles()
        {
            if (_roles == null || _roles.Length == 0 || _roles.Any(r => r == null)) throw new InvalidOperationException("Catalog needs role assets.");
            var roles = _roles.Select(r => r.ToSpec()).ToList();
            if (roles.Select(r => r.Id).Distinct().Count() != roles.Count) throw new InvalidOperationException("Duplicate role ID in catalog.");
            return roles.AsReadOnly();
        }

        /// <summary>Loads the modern role set; old catalogs retain their explicitly referenced definitions.</summary>
        public IReadOnlyList<RoleSpec> LoadMissionRoles()
        {
            if (_missionRoles == null || _missionRoles.Length == 0) return LoadRoles();
            if (_missionRoles.Any(r => r == null)) throw new InvalidOperationException("Mission role reference missing.");
            var roles = _missionRoles.Select(r => r.ToSpec()).ToList();
            if (roles.Select(r => r.Id).Distinct().Count() != roles.Count) throw new InvalidOperationException("Duplicate mission role ID.");
            return roles.AsReadOnly();
        }

        /// <summary>Loads authored dialogue and checks its role-condition references.</summary>
        public IReadOnlyList<DialogueLine> LoadDialogue()
        {
            if (_dialogue == null) throw new InvalidOperationException("Catalog needs a dialogue JSON asset.");
            var lines = DialogueDocument.Parse(_dialogue.text);
            var roles = LoadRoles();
            foreach (DialogueLine line in lines)
            {
                if (line.SpeakerRole.HasValue && !roles.Any(r => r.Id == line.SpeakerRole.Value))
                    throw new InvalidOperationException($"{line.Id}: unknown speaker role condition.");
                foreach (DialogueCondition condition in line.Conditions.Where(c => c.Kind == DialogueConditionKind.SpeakerRole))
                    if (!roles.Any(r => r.Id.Value == condition.Value)) throw new InvalidOperationException($"{line.Id}: unknown speaker role condition {condition.Value}.");
            }
            return lines;
        }

        /// <summary>Copies the cast-name pool so sessions cannot mutate asset state.</summary>
        public IReadOnlyList<string> LoadNames()
        {
            if (_names == null || _names.Length < 3 || _names.Any(string.IsNullOrWhiteSpace)
                || _names.Distinct(StringComparer.Ordinal).Count() != _names.Length)
                throw new InvalidOperationException("Catalog needs at least three distinct nonempty names.");
            return _names.ToList().AsReadOnly();
        }

        /// <summary>Loads optional tuning; older catalogs retain documented defaults.</summary>
        public ReasoningSettings LoadReasoning() => _reasoning != null ? _reasoning.ToSettings() : new ReasoningSettings();
    }
}
