using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Strict semantic/parameterized span parser. Text remains opaque to the rules assembly.</summary>
    public static class SemanticMarkup
    {
        /// <summary>Parses nested styles/effects with bounded parameters and a supplied immutable style catalog.</summary>
        public static SemanticText Parse(string source, GlyphStyleCatalog styles = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Length > 32768) throw new ArgumentException("A source line exceeds 32768 UTF-16 units.");
            styles = styles ?? GlyphStyleCatalog.Default;
            var text = new StringBuilder(source.Length); var spans = new List<SemanticSpan>(); var effects = new List<GlyphEffectSpan>();
            var open = new Stack<(string Tag, int Start, int StyleSlot, int FirstEffect, int Count)>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != '<') { text.Append(source[i]); continue; }
                if (i + 1 < source.Length && source[i + 1] == '<') { text.Append('<'); i++; continue; }
                int end = source.IndexOf('>', i + 1);
                if (end < 0) throw new ArgumentException("Unclosed markup tag.");
                string tag = source.Substring(i + 1, end - i - 1);
                bool closing = tag.StartsWith("/", StringComparison.Ordinal);
                if (closing)
                {
                    string name = tag.Substring(1);
                    if (open.Count == 0 || open.Peek().Tag != name) throw new ArgumentException("Mismatched markup tags.");
                    var entry = open.Pop();
                    if (entry.StyleSlot >= 0) spans[entry.StyleSlot] = new SemanticSpan(entry.Start, text.Length - entry.Start, spans[entry.StyleSlot].Style);
                    for (int n = 0; n < entry.Count; n++)
                    {
                        int slot = entry.FirstEffect + n; GlyphEffectSpan effect = effects[slot];
                        effects[slot] = new GlyphEffectSpan(entry.Start, text.Length - entry.Start, effect.Effect);
                    }
                }
                else
                {
                    if (open.Count >= 8) throw new ArgumentException("Markup nesting exceeds eight levels.");
                    string[] parts = tag.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) throw new ArgumentException("Empty markup tag.");
                    string name = parts[0]; int firstEffect = effects.Count, styleSlot = -1;
                    if (parts.Length == 1 && styles.Append(name, text.Length, effects, out SemanticStyle style))
                    { styleSlot = spans.Count; spans.Add(new SemanticSpan(text.Length, 0, style)); }
                    else effects.Add(new GlyphEffectSpan(text.Length, 0, ParseEffect(parts)));
                    if (effects.Count > 64) throw new ArgumentException("A line supports at most 64 effect spans.");
                    open.Push((name, text.Length, styleSlot, firstEffect, effects.Count - firstEffect));
                }
                i = end;
            }
            if (open.Count != 0) throw new ArgumentException("Unclosed markup tag.");
            if (text.Length > 2048) throw new ArgumentException("A display line exceeds the 2048-character glyph budget.");
            return new SemanticText(text.ToString(), spans, effects);
        }
        private static GlyphEffect ParseEffect(string[] parts)
        {
            if (!Enum.TryParse(parts[0].Replace("-", ""), true, out GlyphEffectKind kind)
                || !Enum.IsDefined(typeof(GlyphEffectKind), kind) || char.IsDigit(parts[0][0])) throw new ArgumentException("Unknown effect tag: " + parts[0]);
            float intensity = 1, speed = 1, amplitude = 3, duration = 1; EffectLifetime lifetime = EffectLifetime.Continuous;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int n = 1; n < parts.Length; n++)
            {
                string[] pair = parts[n].Split('=');
                if (pair.Length != 2 || !seen.Add(pair[0])) throw new ArgumentException("Malformed/duplicate effect parameter.");
                if (pair[0] == "mode")
                {
                    if (!Enum.TryParse(pair[1], true, out lifetime) || !Enum.IsDefined(typeof(EffectLifetime), lifetime)
                        || char.IsDigit(pair[1][0])) throw new ArgumentException("Unknown effect lifetime.");
                    continue;
                }
                if (!float.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) throw new ArgumentException("Effect parameter must be numeric.");
                switch (pair[0])
                { case "i": intensity = value; break; case "s": speed = value; break; case "a": amplitude = value; break; case "d": duration = value; break;
                    default: throw new ArgumentException("Unknown effect parameter: " + pair[0]); }
            }
            if (seen.Contains("d") && !seen.Contains("mode")) lifetime = EffectLifetime.Timed;
            return new GlyphEffect(kind, intensity, speed, amplitude, duration, lifetime);
        }
    }
}
