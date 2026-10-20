using System;
using System.Collections.Generic;
using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Optional inspector overrides for known semantic style recipes; no rules changes required.</summary>
    [CreateAssetMenu(menuName = "SocialStim/Text Style Recipes")]
    public sealed class TextStyleDefinition : ScriptableObject
    {
        [Serializable]
        private sealed class Recipe
        {
            [SerializeField] internal string Tag;
            [SerializeField] internal GlyphEffectKind Effect;
            [SerializeField, Range(0, 4)] internal float Intensity = 1;
            [SerializeField, Range(0.1f, 20)] internal float Speed = 1;
            [SerializeField, Range(0, 20)] internal float Amplitude = 3;
            [SerializeField, Range(0.05f, 30)] internal float Duration = 1;
            [SerializeField] internal EffectLifetime Lifetime;
            internal GlyphEffect Load() => new GlyphEffect(Effect, Intensity, Speed, Amplitude, Duration, Lifetime);
        }
        [SerializeField] private Recipe[] _recipes = Array.Empty<Recipe>();
        /// <summary>Copies up to four entries per semantic tag into a validated immutable catalog.</summary>
        public GlyphStyleCatalog Load()
        {
            var grouped = new Dictionary<string, List<GlyphEffect>>(StringComparer.Ordinal);
            foreach (Recipe recipe in _recipes)
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.Tag)) throw new ArgumentException("Missing text style recipe.");
                if (!grouped.TryGetValue(recipe.Tag, out List<GlyphEffect> effects)) { effects = new List<GlyphEffect>(); grouped.Add(recipe.Tag, effects); }
                effects.Add(recipe.Load());
            }
            var overrides = new Dictionary<string, GlyphEffect[]>();
            foreach (var group in grouped) overrides.Add(group.Key, group.Value.ToArray());
            return new GlyphStyleCatalog(overrides);
        }
    }
}
