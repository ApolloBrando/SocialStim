using System;
using System.Collections;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Native clipping needs a renderer before a custom graphic joins the mask registry.</summary>
    public sealed class CustomGraphicRendererTests
    {
        [UnityTest]
        public IEnumerator TmpCanvasRebuildDefersUnderlineDirtyingAndStillRefreshesReflowedGeometry()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("TMP graphic rebuild regression", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(root.transform, false);
                ((RectTransform)viewport.transform).sizeDelta = new Vector2(400, 200);
                var textRoot = new GameObject("Dialogue", typeof(RectTransform)); textRoot.transform.SetParent(viewport.transform, false);
                var label = textRoot.AddComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset;
                label.rectTransform.sizeDelta = new Vector2(350, 180); label.fontSize = 18;
                var renderer = textRoot.AddComponent<TextEffectRenderer>(); renderer.Initialize(label);
                var document = ActionAppearance.Label(SemanticText.Literal("Ada: I suspect Bram."), SpeechIntent.Accuse, GameText.Default);
                renderer.SetDocument(document, 0, new TextPalette(false)); renderer.Apply(int.MaxValue, default, 0);
                var underlines = textRoot.GetComponentInChildren<ActionUnderlineGraphic>();
                bool dirtiedDuringRebuild = false, capturedDuringRebuild = false;
                underlines.RegisterDirtyVerticesCallback(() => dirtiedDuringRebuild |= CanvasUpdateRegistry.IsRebuildingGraphics());
                label.OnPreRenderText += _ => capturedDuringRebuild |= CanvasUpdateRegistry.IsRebuildingGraphics();
                label.fontSize = 20; label.rectTransform.sizeDelta = new Vector2(160, 180); label.SetVerticesDirty();
                Canvas.ForceUpdateCanvases();
                Assert.That(capturedDuringRebuild, Is.True, "Exercise TMP's registered canvas rebuild, not only ForceMeshUpdate.");
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(dirtiedDuringRebuild, Is.False);
                Mesh mesh = underlines.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(typeof(ActionUnderlineGraphic))]
        [TestCase(typeof(DaylightBackdrop))]
        [TestCase(typeof(ScreenEffectOverlay))]
        public void AddingCustomGraphicCreatesItsRendererBeforeClipping(Type graphicType)
        {
            var root = new GameObject("Custom graphic clipping", typeof(RectTransform), typeof(Canvas), typeof(RectMask2D));
            try
            {
                var child = new GameObject("Graphic", typeof(RectTransform)); child.transform.SetParent(root.transform, false);
                var graphic = (MaskableGraphic)child.AddComponent(graphicType);
                Assert.That(child.GetComponent<CanvasRenderer>(), Is.Not.Null);
                Assert.That(graphic.canvasRenderer, Is.SameAs(child.GetComponent<CanvasRenderer>()));
                Assert.DoesNotThrow(() => graphic.SetClipRect(new Rect(0, 0, 100, 100), true));
                Assert.DoesNotThrow(() => { root.GetComponent<RectMask2D>().PerformClipping(); Canvas.ForceUpdateCanvases(); });
                child.SetActive(false); child.SetActive(true);
                Assert.DoesNotThrow(() => graphic.SetClipRect(new Rect(0, 0, 80, 80), true));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void TextRendererCreatesOneNonInteractiveUnderlineWithItsOwnRenderer()
        {
            var root = new GameObject("Text underline composition", typeof(RectTransform));
            try
            {
                var label = root.AddComponent<TextMeshProUGUI>();
                var renderer = root.AddComponent<TextEffectRenderer>();
                renderer.Initialize(label); renderer.Initialize(label);
                var underlines = root.GetComponentsInChildren<ActionUnderlineGraphic>();
                Assert.That(underlines.Length, Is.EqualTo(1));
                Assert.That(underlines[0].name, Is.EqualTo("Action underlines"));
                Assert.That(underlines[0].GetComponent<CanvasRenderer>(), Is.Not.Null);
                Assert.That(underlines[0].raycastTarget, Is.False);
                Assert.DoesNotThrow(() => underlines[0].SetClipRect(new Rect(0, 0, 100, 100), true));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
