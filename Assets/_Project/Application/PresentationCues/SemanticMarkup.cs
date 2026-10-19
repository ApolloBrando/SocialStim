using System;
using System.Collections.Generic;
using System.Text;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Strict, bounded semantic markup parser. A doubled less-than sign is literal.</summary>
    public static class SemanticMarkup
    {
        public static SemanticText Parse(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Length > 32768) throw new ArgumentException("A display line exceeds 32768 UTF-16 units.");
            var text = new StringBuilder(source.Length);
            var spans = new List<SemanticSpan>();
            var open = new Stack<(string Tag, int Start, int Slot)>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != '<') { text.Append(source[i]); continue; }
                if (i + 1 < source.Length && source[i + 1] == '<') { text.Append('<'); i++; continue; }
                int end = source.IndexOf('>', i + 1);
                if (end < 0) throw new ArgumentException("Unclosed semantic tag.");
                string tag = source.Substring(i + 1, end - i - 1);
                bool closing = tag.StartsWith("/", StringComparison.Ordinal);
                string name = closing ? tag.Substring(1) : tag;
                SemanticStyle style = Style(name);
                if (closing)
                {
                    if (open.Count == 0 || open.Peek().Tag != name) throw new ArgumentException("Mismatched semantic tags.");
                    var entry = open.Pop();
                    spans[entry.Slot] = new SemanticSpan(entry.Start, text.Length - entry.Start, style);
                }
                else
                {
                    if (open.Count >= 8) throw new ArgumentException("Semantic nesting exceeds eight levels.");
                    open.Push((name, text.Length, spans.Count));
                    spans.Add(new SemanticSpan(text.Length, 0, style));
                }
                i = end;
            }
            if (open.Count != 0) throw new ArgumentException("Unclosed semantic tag.");
            return new SemanticText(text.ToString(), spans);
        }

        private static SemanticStyle Style(string tag)
        {
            switch (tag)
            {
                case "emphasis": return SemanticStyle.Emphasis;
                case "fear": return SemanticStyle.Fear;
                case "calm": return SemanticStyle.Calm;
                case "evidence": return SemanticStyle.Evidence;
                case "danger": return SemanticStyle.Danger;
                default: throw new ArgumentException("Unknown semantic tag: " + tag);
            }
        }
    }
}
