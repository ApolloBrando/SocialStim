using System;
using NUnit.Framework;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Native clipping needs a renderer before a custom graphic joins the mask registry.</summary>
    public sealed class CustomGraphicRendererTests
    {
        [TestCase(typeof(ActionUnderlineGraphic))]
        [TestCase(typeof(DaylightBackdrop))]
        [TestCase(typeof(ScreenEffectOverlay))]
        [TestCase(typeof(RelationArrowGraphic))]
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
