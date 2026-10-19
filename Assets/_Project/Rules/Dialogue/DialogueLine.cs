using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>One authored opaque text record and its selection conditions.</summary>
    public sealed class DialogueLine
    {
        /// <summary>Creates a validated line; total pool weights must fit an integer.</summary>
        public DialogueLine(string id, SpeechIntent intent, string text, int weight = 1,
            Personality requiredTrait = Personality.None, RoleId? speakerRole = null,
            DialogueMetadata metadata = null, IEnumerable<DialogueCondition> conditions = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(text) || weight < 1 || weight > 1000)
                throw new ArgumentException("Dialogue requires an ID, text, and weight in [1, 1000].");
            Id = id; Intent = intent; Text = text; Weight = weight;
            RequiredTrait = requiredTrait; SpeakerRole = speakerRole; Metadata = metadata ?? new DialogueMetadata();
            Conditions = (conditions ?? Enumerable.Empty<DialogueCondition>()).ToList().AsReadOnly();
            if (Conditions.Any(c => c == null)) throw new ArgumentException($"{id}: null condition.");
            int firstDay = intent == SpeechIntent.ReportInvestigation ? 2 : 1;
            int minimum = Math.Max(firstDay, Conditions.Where(c => c.Kind == DialogueConditionKind.MinimumDay).Select(c => c.Number.Value).DefaultIfEmpty(firstDay).Max());
            int maximum = Conditions.Where(c => c.Kind == DialogueConditionKind.MaximumDay).Select(c => c.Number.Value).DefaultIfEmpty(int.MaxValue).Min();
            if (minimum > maximum || Conditions.Where(c => c.Kind == DialogueConditionKind.Phase).Select(c => c.Value.ToLowerInvariant()).Distinct().Count() > 1
                || Conditions.Any(c => c.Kind == DialogueConditionKind.Phase && !SpeechContract.AllowsPhase(intent, (GamePhase)Enum.Parse(typeof(GamePhase), c.Value, true)))
                || Conditions.Any(c => c.Kind == DialogueConditionKind.HasTarget && c.Flag.Value != SpeechContract.RequiresTarget(intent))
                || Conditions.Any(c => c.Kind == DialogueConditionKind.HasAssertion && c.Flag.Value != SpeechContract.HasAssertion(intent))
                || Conditions.Any(c => (c.Kind == DialogueConditionKind.TargetAccused || c.Kind == DialogueConditionKind.TargetHasClaim) && !SpeechContract.RequiresTarget(intent)))
                throw new ArgumentException($"{id}: impossible selection conditions.");
            var ownRoles = Conditions.Where(c => c.Kind == DialogueConditionKind.SpeakerRole).Select(c => c.Value).ToList();
            if (speakerRole.HasValue) ownRoles.Add(speakerRole.Value.Value);
            if (ownRoles.Distinct(StringComparer.Ordinal).Count() > 1) throw new ArgumentException($"{id}: contradictory own-role conditions.");
        }
        /// <summary>Stable authored line identity.</summary>
        public string Id { get; }
        /// <summary>Semantic speech intent.</summary>
        public SpeechIntent Intent { get; }
        /// <summary>Text remains opaque except for explicit variable substitution.</summary>
        public string Text { get; }
        /// <summary>Positive integer selection weight.</summary>
        public int Weight { get; }
        /// <summary>All required speaker traits.</summary>
        public Personality RequiredTrait { get; }
        /// <summary>Optional condition on the speaker's own known role.</summary>
        public RoleId? SpeakerRole { get; }
        /// <summary>Future presentation hints.</summary>
        public DialogueMetadata Metadata { get; }
        /// <summary>AND-combined registered predicates; empty means no additional restriction.</summary>
        public IReadOnlyList<DialogueCondition> Conditions { get; }
    }
}
