using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Randomness;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Seeded weighted selection with condition-safe per-speaker anti-repetition.</summary>
    public sealed class DialogueSelector
    {
        private readonly IReadOnlyList<DialogueLine> _lines;
        private readonly IRandomSource _random;
        private readonly Dictionary<CharacterId, Queue<string>> _recent = new Dictionary<CharacterId, Queue<string>>();
        private static readonly Regex Variables = new Regex(@"\{([^{}]*)\}");
        private static readonly HashSet<string> AllowedVariables = new HashSet<string> { "speaker", "target", "role", "result", "day" };

        /// <summary>Validates content and generic fallback coverage for every registered intent.</summary>
        public DialogueSelector(IEnumerable<DialogueLine> lines, IRandomSource random)
        {
            _lines = lines.OrderBy(l => l.Id, StringComparer.Ordinal).ToList().AsReadOnly();
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (_lines.Select(l => l.Id).Distinct(StringComparer.Ordinal).Count() != _lines.Count)
                throw new ArgumentException("Duplicate dialogue ID.");
            foreach (DialogueLine line in _lines)
                foreach (Match match in Variables.Matches(line.Text))
                    if (!AllowedVariables.Contains(match.Groups[1].Value)) throw new ArgumentException($"{line.Id}: unknown variable {match.Value}.");
            foreach (SpeechIntent intent in Enum.GetValues(typeof(SpeechIntent)))
                if (!_lines.Any(l => l.Intent == intent && l.RequiredTrait == Personality.None && !l.SpeakerRole.HasValue && l.Conditions.Count == 0))
                    throw new ArgumentException($"Missing unrestricted dialogue fallback for {intent}.");
            foreach (var group in _lines.GroupBy(l => l.Intent)) checked { _ = group.Sum(l => l.Weight); }
        }

        /// <summary>Selects a line without ever relaxing role/trait eligibility.</summary>
        public DialogueSelection Select(SpeechIntent intent, DialogueContext context)
        {
            var eligible = _lines.Where(l => l.Intent == intent && (context.Traits & l.RequiredTrait) == l.RequiredTrait
                && (!l.SpeakerRole.HasValue || l.SpeakerRole.Value == context.OwnRole) && l.Conditions.All(c => c.Matches(context))).ToList();
            if (eligible.Count == 0) throw new InvalidOperationException($"No eligible line for {intent}.");
            if (!_recent.TryGetValue(context.SpeakerId, out Queue<string> history))
            { history = new Queue<string>(); _recent.Add(context.SpeakerId, history); }
            var candidates = eligible.Where(l => !history.Contains(l.Id)).ToList();
            while (candidates.Count == 0)
            { history.Dequeue(); candidates = eligible.Where(l => !history.Contains(l.Id)).ToList(); }
            int ticket = _random.Next(candidates.Sum(l => l.Weight));
            DialogueLine chosen = candidates[0];
            foreach (DialogueLine candidate in candidates)
            { ticket -= candidate.Weight; if (ticket < 0) { chosen = candidate; break; } }
            history.Enqueue(chosen.Id);
            while (history.Count > 3) history.Dequeue();
            string text = Variables.Replace(chosen.Text, match =>
            {
                switch (match.Groups[1].Value)
                {
                    case "speaker": return context.Speaker;
                    case "target": return context.Target;
                    case "role": return context.AssertedRole;
                    case "result": return context.AssertedResult;
                    default: return context.Day.ToString(CultureInfo.InvariantCulture);
                }
            });
            return new DialogueSelection(chosen.Id, text, chosen.Metadata);
        }
    }
}
