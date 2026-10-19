using SocialStim.Application.PresentationCues;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Overlay-only border, feathered vignette, and low-opacity flash. Never intercepts input.</summary>
    public sealed class ScreenEffectOverlay : MaskableGraphic
    {
        private EffectFrame _frame;
        private Color _ink = Color.white;
        public void Apply(EffectFrame frame, TextPalette palette)
        {
            if (_frame.Border == frame.Border && _frame.Vignette == frame.Vignette && _frame.Flash == frame.Flash && _ink == palette.Foreground) return;
            _frame = frame; _ink = palette.Foreground; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            if (_frame.Border > 0) Ring(mesh, bounds, 2, _frame.Border, _frame.Border);
            if (_frame.Vignette > 0)
            {
                float depth = Mathf.Min(bounds.width, bounds.height) * 0.07f;
                const int rings = 8;
                for (int i = 0; i < rings; i++)
                {
                    Rect ring = new Rect(bounds.xMin + depth * i / rings, bounds.yMin + depth * i / rings,
                        bounds.width - depth * i * 2 / rings, bounds.height - depth * i * 2 / rings);
                    Ring(mesh, ring, depth / rings, _frame.Vignette * 0.2f * (1 - i / (float)rings),
                        _frame.Vignette * 0.2f * (1 - (i + 1) / (float)rings));
                }
            }
            if (_frame.Flash > 0) Quad(mesh, bounds, _frame.Flash);
        }
        private void Ring(VertexHelper mesh, Rect bounds, float thickness, float outerAlpha, float innerAlpha)
        {
            int start = mesh.currentVertCount;
            Color outer = _ink; outer.a = outerAlpha;
            Color inner = _ink; inner.a = innerAlpha;
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
        private void Quad(VertexHelper mesh, Rect bounds, float alpha)
        {
            int start = mesh.currentVertCount;
            Color tint = _ink; tint.a = alpha;
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMin, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMax, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMax, 0), tint, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMin, 0), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
