using SocialStim.Application.PresentationCues;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Native underline geometry for action labels, retaining literal text and semantic indices.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ActionUnderlineGraphic : MaskableGraphic
    {
        private TextMeshProUGUI _source;
        private SemanticText _text;
        private int _visible;
        public void Configure(TextMeshProUGUI source, SemanticText text, int visible, Color tint)
        {
            bool changed = _source != source || _text != text || _visible != visible || color != tint;
            _source = source; _text = text; _visible = visible; color = tint;
            if (changed) SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if (_source == null || _text == null) return;
            var info = _source.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i]; if (c.index >= _visible) break;
                var style = _text.StyleAt(c.index);
                if ((int)style < (int)SemanticStyle.ActionPressure || (int)style > (int)SemanticStyle.ActionEvidence || char.IsWhiteSpace(c.character)) continue;
                float y = c.baseLine - _source.fontSize * 0.13f;
                float thickness = Mathf.Max(1, _source.fontSize * 0.055f);
                int v = mesh.currentVertCount;
                mesh.AddVert(new Vector3(c.origin, y, 0), color, Vector2.zero);
                mesh.AddVert(new Vector3(c.origin, y + thickness, 0), color, Vector2.zero);
                mesh.AddVert(new Vector3(c.xAdvance, y + thickness, 0), color, Vector2.zero);
                mesh.AddVert(new Vector3(c.xAdvance, y, 0), color, Vector2.zero);
                mesh.AddTriangle(v, v + 1, v + 2); mesh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
