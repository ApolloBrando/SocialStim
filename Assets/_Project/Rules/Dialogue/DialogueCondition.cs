using System;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>One validated declarative condition; no executable expressions or target secrets.</summary>
    public sealed class DialogueCondition
    {
        private readonly GamePhase _phase;
        private readonly Personality _trait;

        /// <summary>Requires exactly the parameter appropriate to the registered predicate.</summary>
        public DialogueCondition(DialogueConditionKind kind, string value = null, int? number = null, bool? flag = null)
        {
            if (!Enum.IsDefined(typeof(DialogueConditionKind), kind)) throw new ArgumentException("Unknown condition kind.");
            bool textKind = kind == DialogueConditionKind.Phase || kind == DialogueConditionKind.SpeakerRole || kind == DialogueConditionKind.SpeakerTrait;
            bool numberKind = kind == DialogueConditionKind.MinimumDay || kind == DialogueConditionKind.MaximumDay || kind == DialogueConditionKind.SpeakerUnderPressure;
            bool flagKind = kind == DialogueConditionKind.HasTarget || kind == DialogueConditionKind.HasAssertion;
            if (textKind != (value != null) || numberKind != number.HasValue || flagKind != flag.HasValue)
                throw new ArgumentException($"Wrong parameters for {kind}.");
            if (textKind && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Condition value is empty.");
            if (kind == DialogueConditionKind.Phase && (!Enum.TryParse(value, true, out _phase) || !Enum.IsDefined(typeof(GamePhase), _phase)))
                throw new ArgumentException("Unknown phase condition.");
            if (kind == DialogueConditionKind.SpeakerTrait && (!Enum.TryParse(value, true, out _trait)
                || (_trait & ~(Personality.Aggressive | Personality.Cautious | Personality.Gullible | Personality.Loyal | Personality.Deceptive)) != 0))
                throw new ArgumentException("Unknown trait condition.");
            if (numberKind && (number < 0 || kind != DialogueConditionKind.SpeakerUnderPressure && number < 1
                || kind == DialogueConditionKind.SpeakerUnderPressure && number > 100)) throw new ArgumentException("Invalid condition range.");
            Kind = kind; Value = value; Number = number; Flag = flag;
        }
        /// <summary>Registered predicate.</summary>
        public DialogueConditionKind Kind { get; }
        /// <summary>Optional phase, trait, or own-role value.</summary>
        public string Value { get; }
        /// <summary>Optional day/pressure threshold.</summary>
        public int? Number { get; }
        /// <summary>Optional target/assertion presence requirement.</summary>
        public bool? Flag { get; }

        /// <summary>Evaluates only authorized facts already present in the context.</summary>
        public bool Matches(DialogueContext context)
        {
            switch (Kind)
            {
                case DialogueConditionKind.Phase: return context.Phase == _phase;
                case DialogueConditionKind.MinimumDay: return context.Day >= Number.Value;
                case DialogueConditionKind.MaximumDay: return context.Day <= Number.Value;
                case DialogueConditionKind.HasTarget: return context.HasTarget == Flag.Value;
                case DialogueConditionKind.HasAssertion: return context.HasAssertion == Flag.Value;
                case DialogueConditionKind.SpeakerTrait: return (context.Traits & _trait) == _trait;
                case DialogueConditionKind.SpeakerRole: return context.OwnRole == new RoleId(Value);
                case DialogueConditionKind.SpeakerUnderPressure: return context.SelfExposure >= Number.Value;
                case DialogueConditionKind.ObservedDeath: return context.Evidence.Any(e => e.Kind == EvidenceKind.Death);
                case DialogueConditionKind.ObservedRoleReveal: return context.Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal);
                case DialogueConditionKind.TargetAccused: return context.TargetId.HasValue && context.Evidence.Any(e => e.Target == context.TargetId
                    && e.Kind == EvidenceKind.Statement && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.CallForTrial || e.Intent == SpeechIntent.Deflect));
                case DialogueConditionKind.TargetHasClaim: return context.TargetId.HasValue && context.Evidence.Any(e => e.Actor == context.TargetId && e.Kind == EvidenceKind.RoleClaim);
                default: return false;
            }
        }
    }
}
