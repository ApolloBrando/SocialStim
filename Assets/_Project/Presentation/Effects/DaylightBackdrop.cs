using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Native uGUI sky gradient with a short colour crossfade; never advances game time.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DaylightBackdrop : MaskableGraphic
    {
        private Color _top, _bottom, _fromTop, _fromBottom, _toTop, _toBottom;
        private float _elapsed = 1.2f;
        private bool _initialized;
        public void Apply(TextPalette palette, bool immediate = false)
        {
            if (_initialized && _toTop == palette.SkyTop && _toBottom == palette.SkyBottom) return;
            _fromTop = _top; _fromBottom = _bottom; _toTop = palette.SkyTop; _toBottom = palette.SkyBottom;
            _elapsed = !_initialized || immediate ? 1.2f : 0;
            _initialized = true;
            if (_elapsed >= 1.2f) { _top = _toTop; _bottom = _toBottom; SetVerticesDirty(); }
        }
        private void Update()
        {
            if (_elapsed >= 1.2f) return;
            _elapsed = Mathf.Min(1.2f, _elapsed + Time.unscaledDeltaTime);
            float progress = Mathf.SmoothStep(0, 1, _elapsed / 1.2f);
            _top = Color.Lerp(_fromTop, _toTop, progress); _bottom = Color.Lerp(_fromBottom, _toBottom, progress);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect bounds = rectTransform.rect;
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMin, 0), _bottom, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMin, bounds.yMax, 0), _top, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMax, 0), _top, Vector2.zero);
            mesh.AddVert(new Vector3(bounds.xMax, bounds.yMin, 0), _bottom, Vector2.zero);
            mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
        }
    }
}
