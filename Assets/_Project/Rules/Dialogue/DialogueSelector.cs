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
        private readonly Dictionary<(SpeechIntent, Personality), IReadOnlyList<DialogueLine>> _pools
            = new Dictionary<(SpeechIntent, Personality), IReadOnlyList<DialogueLine>>();
        private readonly Dictionary<CharacterId, Queue<string>> _recent = new Dictionary<CharacterId, Queue<string>>();
        private readonly Dictionary<(CharacterId, SpeechIntent), HashSet<string>> _used = new Dictionary<(CharacterId, SpeechIntent), HashSet<string>>();
        private readonly Dictionary<string, int> _uses = new Dictionary<string, int>();
        private static readonly Regex Variables = new Regex(@"\{([^{}]*)\}");
        private static readonly HashSet<string> AllowedVariables = new HashSet<string> { "speaker", "target", "role", "result", "day" };

        /// <summary>Validates content and same-trait fallback coverage for every represented voice.</summary>
        public DialogueSelector(IEnumerable<DialogueLine> lines, IRandomSource random)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var source = lines.ToList();
            if (source.Any(line => line == null)) throw new ArgumentException("Null dialogue line.", nameof(lines));
            _lines = source.OrderBy(l => l.Id, StringComparer.Ordinal).ToList().AsReadOnly();
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (_lines.Select(l => l.Id).Distinct(StringComparer.Ordinal).Count() != _lines.Count)
                throw new ArgumentException("Duplicate dialogue ID.");
            foreach (DialogueLine line in _lines)
            {
                foreach (Match match in Variables.Matches(line.Text))
                    if (!AllowedVariables.Contains(match.Groups[1].Value)) throw new ArgumentException($"{line.Id}: unknown variable {match.Value}.");
                // A valid-token regex alone misses unmatched and nested braces, which
                // otherwise survive substitution and appear verbatim in the game.
                string literal = Variables.Replace(line.Text, "");
                if (literal.IndexOf('{') >= 0 || literal.IndexOf('}') >= 0)
                    throw new ArgumentException($"{line.Id}: malformed placeholder.");
            }
            if (_lines.Count == 0) throw new ArgumentException("Dialogue catalogue is empty.");
            foreach (var group in _lines.GroupBy(line => (line.Intent, line.RequiredTrait)))
            {
                var pool = group.ToList().AsReadOnly();
                checked { _ = pool.Sum(line => line.Weight); }
                _pools.Add(group.Key, pool);
            }
            foreach (Personality trait in _lines.Select(line => line.RequiredTrait).Distinct())
                foreach (SpeechIntent intent in Enum.GetValues(typeof(SpeechIntent)))
                    if (!_pools.TryGetValue((intent, trait), out var pool)
                        || !pool.Any(line => !line.SpeakerRole.HasValue && line.Conditions.Count == 0))
                        throw new ArgumentException($"Missing unrestricted {trait}/{intent} dialogue fallback.");
        }

        /// <summary>Selects a line without ever relaxing role/trait eligibility.</summary>
        public DialogueSelection Select(SpeechIntent intent, DialogueContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!_pools.TryGetValue((intent, context.Traits), out var pool))
                throw new InvalidOperationException($"No dialogue pool for {context.Traits}/{intent}.");
            // Trait is an immutable pool boundary: None is the player's pool, never
            // an NPC fallback. Role/evidence/day gates are never discarded either.
            var matching = pool.Where(line => (!line.SpeakerRole.HasValue || line.SpeakerRole.Value == context.OwnRole)
                && line.Conditions.All(condition => condition.Matches(context))).ToList();
            if (matching.Count == 0) throw new InvalidOperationException($"No eligible {context.Traits} line for {intent}.");
            // Prefer the most context-specific matching tier, then broader matching
            // tiers, ending in this same trait's unrestricted action pool.
            var key = (context.SpeakerId, intent);
            if (!_used.TryGetValue(key, out var used)) { used = new HashSet<string>(); _used.Add(key, used); }
            var fresh = matching.Where(line => !used.Contains(line.Id)).ToList();
            if (fresh.Count == 0) { used.Clear(); fresh = matching; }
            int specificity = fresh.Max(line => line.Conditions.Count + (line.SpeakerRole.HasValue ? 1 : 0));
            var eligible = fresh.Where(line => line.Conditions.Count + (line.SpeakerRole.HasValue ? 1 : 0) == specificity).ToList();
            if (!_recent.TryGetValue(context.SpeakerId, out Queue<string> history))
            { history = new Queue<string>(); _recent.Add(context.SpeakerId, history); }
            var candidates = eligible.Where(l => !history.Contains(l.Id)).ToList();
            while (candidates.Count == 0)
            { history.Dequeue(); candidates = eligible.Where(l => !history.Contains(l.Id)).ToList(); }
            int leastUsed = candidates.Min(line => _uses.TryGetValue(line.Id, out int count) ? count : 0);
            candidates = candidates.Where(line => (_uses.TryGetValue(line.Id, out int count) ? count : 0) == leastUsed).ToList();
            int ticket = _random.Next(candidates.Sum(l => l.Weight));
            DialogueLine chosen = candidates[0];
            foreach (DialogueLine candidate in candidates)
            { ticket -= candidate.Weight; if (ticket < 0) { chosen = candidate; break; } }
            history.Enqueue(chosen.Id);
            used.Add(chosen.Id); _uses[chosen.Id] = leastUsed + 1;
            while (history.Count > 12) history.Dequeue();
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
