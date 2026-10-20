using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Immutable authored copy. Missing entries/tokens fail explicitly; there is no prose fallback in code.</summary>
    public sealed class TextTemplates
    {
        private static readonly Regex Tokens = new Regex(@"\{([A-Za-z][A-Za-z0-9]*)\}");
        private readonly IReadOnlyDictionary<string, string> _entries;
        /// <summary>Validates and detaches a set of stable keys and named-token templates.</summary>
        public TextTemplates(IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                    throw new ArgumentException("Text entries need nonempty IDs and text.");
                if (copy.ContainsKey(entry.Key)) throw new ArgumentException("Duplicate text ID: " + entry.Key);
                string remainder = Tokens.Replace(entry.Value, "");
                if (remainder.Contains("{") || remainder.Contains("}")) throw new ArgumentException(entry.Key + ": invalid template token.");
                copy.Add(entry.Key, entry.Value);
            }
            _entries = copy;
        }
        /// <summary>All loaded content IDs, for validation and authoring tools.</summary>
        public IEnumerable<string> Keys => _entries.Keys;
        /// <summary>Formats one authored template with speaker-authorized values, independently of any game RNG.</summary>
        public string Render(string id, params (string Key, object Value)[] values)
        {
            if (!_entries.TryGetValue(id, out string text)) throw new ArgumentException("Missing text ID: " + id);
            var supplied = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (supplied.ContainsKey(value.Key)) throw new ArgumentException(id + ": duplicate token " + value.Key);
                supplied.Add(value.Key, value.Value);
            }
            return Tokens.Replace(text, match => supplied.TryGetValue(match.Groups[1].Value, out object value)
                ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
                : throw new ArgumentException(id + ": missing token " + match.Groups[1].Value));
        }
    }
}
