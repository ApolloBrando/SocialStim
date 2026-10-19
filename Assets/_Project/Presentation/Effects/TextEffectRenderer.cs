using System;
using SocialStim.Application.PresentationCues;
using TMPro;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Cached TMP geometry; no strings, collections or materials created during Apply.</summary>
    public sealed class TextEffectRenderer : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private SemanticText _document;
        private TextPalette _palette;
        private Vector3[][] _vertices = Array.Empty<Vector3[]>();
        private SemanticStyle[] _styles = Array.Empty<SemanticStyle>();
        private int _visible = int.MaxValue;
        private int _animatedStart;
        private EffectFrame _frame;
        private float _time;
        public void Initialize(TextMeshProUGUI label)
        { _label = label; _label.OnPreRenderText += Capture; }
        public void SetDocument(SemanticText document, int animatedStart, TextPalette palette)
        {
            _document = document; _animatedStart = animatedStart; _palette = palette;
            _visible = animatedStart; _frame = default;
            _label.text = document.Text; _label.richText = false;
            _label.ForceMeshUpdate();
        }
        public void SetPalette(TextPalette palette) { _palette = palette; }
        public void Apply(int visibleUtf16, EffectFrame frame, float time)
        {
            if (_label == null || _document == null) return;
            _visible = visibleUtf16; _frame = frame; _time = time;
            Paint(_label.textInfo);
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
        private void Capture(TMP_TextInfo info)
        {
            if (_document == null) return;
            if (_vertices.Length < info.meshInfo.Length) Array.Resize(ref _vertices, info.meshInfo.Length);
            for (int mesh = 0; mesh < info.meshInfo.Length; mesh++)
            {
                Vector3[] source = info.meshInfo[mesh].vertices;
                if (_vertices[mesh] == null || _vertices[mesh].Length < source.Length) _vertices[mesh] = new Vector3[source.Length];
                Array.Copy(source, _vertices[mesh], source.Length);
            }
            if (_styles.Length < info.characterCount) Array.Resize(ref _styles, info.characterCount);
            for (int i = 0; i < info.characterCount; i++) _styles[i] = _document.StyleAt(info.characterInfo[i].index);
            Paint(info);
        }
        private void Paint(TMP_TextInfo info)
        {
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || character.materialReferenceIndex >= _vertices.Length) continue;
                int mesh = character.materialReferenceIndex, vertex = character.vertexIndex;
                Vector3[] baseline = _vertices[mesh];
                if (baseline == null || vertex + 3 >= baseline.Length) continue;
                Color32 color = _palette.Accent(_styles[i]);
                if (character.index >= _visible) color.a = 0;
                float strength = character.index >= _animatedStart ? _frame.Shake : 0;
                Vector3 offset = new Vector3(Mathf.Sin(_time * 27 + i * 1.7f), Mathf.Cos(_time * 31 + i * 1.3f), 0) * (strength * 3);
                for (int corner = 0; corner < 4; corner++)
                {
                    info.meshInfo[mesh].vertices[vertex + corner] = baseline[vertex + corner] + offset;
                    info.meshInfo[mesh].colors32[vertex + corner] = color;
                }
            }
        }
        private void OnDestroy() { if (_label != null) _label.OnPreRenderText -= Capture; }
    }
}
