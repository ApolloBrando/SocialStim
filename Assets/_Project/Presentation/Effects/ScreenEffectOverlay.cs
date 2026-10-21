using SocialStim.Application.PresentationCues;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Situation tint, peripheral glow, dark vignette and limited flash. Never intercepts input.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ScreenEffectOverlay : MaskableGraphic
    {
        private EffectFrame _frame;
        private Color _ink = Color.white;
        public void Apply(EffectFrame frame, TextPalette palette, EffectTone tone = EffectTone.Neutral)
        {
            Color ink = palette.Situation(tone);
            if (_frame.Border == frame.Border && _frame.Vignette == frame.Vignette && _frame.Flash == frame.Flash
                && _frame.ColorWash == frame.ColorWash && _frame.EdgeGlow == frame.EdgeGlow && _ink == ink) return;
            _frame = frame; _ink = ink; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            if (_frame.ColorWash > 0) Quad(mesh, bounds, _ink, _frame.ColorWash);
            if (_frame.Vignette > 0)
            {
                float depth = Mathf.Min(bounds.width, bounds.height) * 0.07f;
                const int rings = 8;
                for (int i = 0; i < rings; i++)
                {
                    Rect ring = new Rect(bounds.xMin + depth * i / rings, bounds.yMin + depth * i / rings,
                        bounds.width - depth * i * 2 / rings, bounds.height - depth * i * 2 / rings);
                    Ring(mesh, ring, depth / rings, Color.Lerp(Color.black, _ink, 0.15f), _frame.Vignette * 0.3f * (1 - i / (float)rings),
                        _frame.Vignette * 0.3f * (1 - (i + 1) / (float)rings));
                }
            }
            if (_frame.EdgeGlow > 0)
            {
                float depth = Mathf.Min(bounds.width, bounds.height) * 0.035f;
                for (int i = 0; i < 8; i++)
                {
                    Rect ring = new Rect(bounds.xMin + depth * i / 8, bounds.yMin + depth * i / 8,
                        bounds.width - depth * i / 4, bounds.height - depth * i / 4);
                    Ring(mesh, ring, depth / 8, _ink, _frame.EdgeGlow * (1 - i / 8f), _frame.EdgeGlow * (1 - (i + 1) / 8f));
                }
            }
            if (_frame.Border > 0) Ring(mesh, bounds, 2, _ink, _frame.Border, _frame.Border);
            if (_frame.Flash > 0) Quad(mesh, bounds, _ink, _frame.Flash);
        }
        private void Ring(VertexHelper mesh, Rect bounds, float thickness, Color tint, float outerAlpha, float innerAlpha)
        {
            int start = mesh.currentVertCount;
            Color outer = tint; outer.a = outerAlpha;
            Color inner = tint; inner.a = innerAlpha;
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMin, 0), outer, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMax, 0), outer, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMax, 0), outer, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMin, 0), outer, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin + thickness, bounds.yMin + thickness, 0), inner, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin + thickness, bounds.yMax - thickness, 0), inner, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax - thickness, bounds.yMax - thickness, 0), inner, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax - thickness, bounds.yMin + thickness, 0), inner, Vector2.zero);
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                mesh.AddTriangle(start + i, start + next, start + 4 + i);
                mesh.AddTriangle(start + next, start + 4 + next, start + 4 + i);
            }
        }
        private void Quad(VertexHelper mesh, Rect bounds, Color tint, float alpha)
        {
            int start = mesh.currentVertCount;
            tint.a = alpha;
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMin, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMax, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMax, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMin, 0), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
