using System;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Requires real TMP resources; these adapter checks cannot run in the headless project.</summary>
    public sealed class TextEffectsAdapterTests
    {
        [TestCase(false, 0f)]
        [TestCase(false, 0.5f)]
        [TestCase(false, 1f)]
        [TestCase(true, 0f)]
        public void SemanticAccentsKeepContrastAgainstThePhaseBackground(bool night, float dayProgress)
        {
            var palette = new TextPalette(night, dayProgress);
            foreach (SemanticStyle style in Enum.GetValues(typeof(SemanticStyle)))
            {
                float foreground = Luminance(palette.Accent(style));
                float background = Luminance(palette.Background);
                float contrast = (Mathf.Max(foreground, background) + 0.05f) / (Mathf.Min(foreground, background) + 0.05f);
                Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), style.ToString());
            }
        }
        [Test]
        public void RevealAndCancelRestoreTmpVertexPositionsAndVisibility()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources before running adapter tests.");
            var canvas = new GameObject("Effects test", typeof(RectTransform), typeof(Canvas));
            try
            {
                var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(canvas.transform, false);
                var label = textObject.GetComponent<TextMeshProUGUI>();
                label.font = TMP_Settings.defaultFontAsset;
                label.rectTransform.sizeDelta = new Vector2(500, 200);
                var renderer = canvas.AddComponent<TextEffectRenderer>(); renderer.Initialize(label);
                renderer.SetDocument(SemanticMarkup.Parse("<fear>AB</fear>"), 0, new TextPalette(false));
                label.ForceMeshUpdate();
                Assert.That(label.textInfo.characterCount, Is.EqualTo(2));
                var character = label.textInfo.characterInfo[0];
                Vector3 baseline = label.textInfo.meshInfo[character.materialReferenceIndex].vertices[character.vertexIndex];
                renderer.Apply(0, default, 0);
                Assert.That(label.textInfo.meshInfo[character.materialReferenceIndex].colors32[character.vertexIndex].a, Is.Zero);
                renderer.Apply(2, new EffectFrame(1, 0, 0, 0), 0.2f);
                Assert.That(label.textInfo.meshInfo[character.materialReferenceIndex].vertices[character.vertexIndex], Is.Not.EqualTo(baseline));
                renderer.Apply(2, default, 0);
                Assert.That(label.textInfo.meshInfo[character.materialReferenceIndex].vertices[character.vertexIndex], Is.EqualTo(baseline));
                Assert.That(label.textInfo.meshInfo[character.materialReferenceIndex].colors32[character.vertexIndex].a, Is.EqualTo(255));
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }
        private static float Luminance(Color color) => Linear(color.r) * 0.2126f + Linear(color.g) * 0.7152f + Linear(color.b) * 0.0722f;
        private static float Linear(float channel) => channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
