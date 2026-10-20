using System;
using System.Collections.Generic;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Immutable semantic-to-effect mapping; Unity assets can replace recipes without changing dialogue.</summary>
    public sealed class GlyphStyleCatalog
    {
        private readonly Dictionary<string, GlyphEffect[]> _recipes = new Dictionary<string, GlyphEffect[]>(StringComparer.Ordinal);
        /// <summary>Default semantic styles, shared safely across parsed documents.</summary>
        public static GlyphStyleCatalog Default { get; } = new GlyphStyleCatalog();
        /// <summary>Creates defaults with optional replacements for known semantic styles.</summary>
        public GlyphStyleCatalog(IDictionary<string, GlyphEffect[]> overrides = null)
        {
            foreach (string tag in new[] { "emphasis", "fear", "calm", "evidence", "danger", "rage", "whisper", "shout", "lie-tell", "dread", "glitch" })
                _recipes.Add(tag, Array.Empty<GlyphEffect>());
            _recipes["fear"] = new[] { new GlyphEffect(GlyphEffectKind.Tremble, 0.5f) };
            _recipes["rage"] = new[] { new GlyphEffect(GlyphEffectKind.Quake), new GlyphEffect(GlyphEffectKind.Pulse, 0.4f) };
            _recipes["whisper"] = new[] { new GlyphEffect(GlyphEffectKind.Whisper) };
            _recipes["shout"] = new[] { new GlyphEffect(GlyphEffectKind.Shout, lifetime: EffectLifetime.Burst) };
            _recipes["lie-tell"] = new[] { new GlyphEffect(GlyphEffectKind.Stutter, 0.3f) };
            _recipes["dread"] = new[] { new GlyphEffect(GlyphEffectKind.Breathe, 0.7f) };
            _recipes["glitch"] = new[] { new GlyphEffect(GlyphEffectKind.Glitch) };
            if (overrides == null) return;
            foreach (var entry in overrides)
            {
                if (!_recipes.ContainsKey(entry.Key) || entry.Value == null || entry.Value.Length > 4)
                    throw new ArgumentException("Styles require a known semantic name and at most four effects.");
                _recipes[entry.Key] = (GlyphEffect[])entry.Value.Clone();
            }
        }
        /// <summary>Appends a style's effects to a document; the internal recipe is never exposed.</summary>
        public bool Append(string tag, int start, List<GlyphEffectSpan> target, out SemanticStyle style)
        {
            style = tag == "fear" || tag == "dread" ? SemanticStyle.Fear : tag == "calm" || tag == "whisper" ? SemanticStyle.Calm
                : tag == "evidence" || tag == "glitch" ? SemanticStyle.Evidence : tag == "danger" || tag == "rage" ? SemanticStyle.Danger
                : tag == "emphasis" || tag == "shout" || tag == "lie-tell" ? SemanticStyle.Emphasis : SemanticStyle.Default;
            if (!_recipes.TryGetValue(tag, out GlyphEffect[] effects)) return false;
            foreach (GlyphEffect effect in effects) target.Add(new GlyphEffectSpan(start, 0, effect));
            return true;
        }
    }
}
