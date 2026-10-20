using System;
using SocialStim.Application.PresentationCues;
using TMPro;
using UnityEngine;
using Unity.Profiling;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Cached TMP span renderer. Parsing/buffer growth happen at document/layout boundaries, never during Apply.</summary>
    public sealed class TextEffectRenderer : MonoBehaviour
    {
        private static readonly ProfilerMarker ApplyMarker = new ProfilerMarker("SocialStim.TextEffects.Apply");
        private static readonly ProfilerMarker CaptureMarker = new ProfilerMarker("SocialStim.TextEffects.Capture");
        private TextMeshProUGUI _label;
        private SemanticText _document;
        private TextPalette _palette;
        private EffectsSettings _settings = new EffectsSettings();
        private Vector3[][] _vertices = Array.Empty<Vector3[]>();
        private SemanticStyle[] _styles = Array.Empty<SemanticStyle>();
        private float[] _revealed = Array.Empty<float>();
        private int _visible = int.MaxValue, _animatedStart;
        private EffectFrame _frame;
        private float _time;
        private bool _semanticColors = true, _stopped;
        /// <summary>Subscribes to TMP rebuilds so cached baselines cannot drift.</summary>
        public void Initialize(TextMeshProUGUI label) { _label = label; _label.OnPreRenderText += Capture; }
        /// <summary>Sets a bounded current line and resets all prior span lifetimes.</summary>
        public void SetDocument(SemanticText document, int animatedStart, TextPalette palette)
        {
            if (document.Text.Length > 2048) throw new ArgumentException("Current line exceeds the glyph budget.");
            _document = document; _animatedStart = animatedStart; _palette = palette; _stopped = false;
            _visible = animatedStart; _frame = default; _time = 0;
            if (_revealed.Length < document.Text.Length) Array.Resize(ref _revealed, document.Text.Length);
            for (int i = 0; i < _revealed.Length; i++) _revealed[i] = -1;
            _label.text = document.Text; _label.richText = false; _label.ForceMeshUpdate();
        }
        /// <summary>Applies phase colors without replacing the document.</summary>
        public void SetPalette(TextPalette palette) { _palette = palette; }
        /// <summary>Enables/disables semantic accents independently of text facts.</summary>
        public void SetSemanticColors(bool enabled) { _semanticColors = enabled; }
        /// <summary>Updates accessibility policy without restarting span ages.</summary>
        public void SetPolicy(EffectsSettings settings) { _settings = settings; }
        /// <summary>Restores geometry on cancellation/dismissal, including post-reveal motion.</summary>
        public void StopEffects() { _stopped = true; if (_document != null) Apply(int.MaxValue, default, _time); }
        /// <summary>Samples cached ranges and uploads existing geometry/colors; creates no strings or collections.</summary>
        public void Apply(int visibleUtf16, EffectFrame frame, float time)
        {
            if (_label == null || _document == null) return;
            using (ApplyMarker.Auto())
            {
                _visible = visibleUtf16; _frame = frame; _time = time;
                Paint(_label.textInfo);
                _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            }
        }
        private void Capture(TMP_TextInfo info)
        {
            if (_document == null) return;
            using (CaptureMarker.Auto())
            {
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
        }
        private void Paint(TMP_TextInfo info)
        {
            int effectBudget = 16384;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || character.materialReferenceIndex >= _vertices.Length) continue;
                int mesh = character.materialReferenceIndex, vertex = character.vertexIndex, index = character.index;
                Vector3[] baseline = _vertices[mesh];
                if (baseline == null || vertex + 3 >= baseline.Length) continue;
                Color color = _palette.Accent(_semanticColors ? _styles[i] : SemanticStyle.Default);
                bool visible = index < _visible;
                if (visible && _revealed[index] < 0) _revealed[index] = _time;
                float x = 0, y = 0, sx = 1, sy = 1, angle = 0, alpha = visible ? 1 : 0;
                if (!_stopped && visible && index >= _animatedStart)
                {
                    float strength = _settings.ReducedMotion || !_settings.Enabled ? 0 : _frame.Shake;
                    x = Mathf.Sin(_time * 27 + i * 1.7f) * strength * 3; y = Mathf.Cos(_time * 31 + i * 1.3f) * strength * 3;
                    for (int n = 0; n < _document.Effects.Count && effectBudget > 0; n++)
                    {
                        GlyphEffectSpan span = _document.Effects[n];
                        if (index < span.Start || index >= span.Start + span.Length) continue;
                        effectBudget--;
                        GlyphTransform sample = GlyphEffectSampler.Sample(span.Effect, Math.Max(0, _time - _revealed[index]), i, _settings, _time);
                        x += sample.X; y += sample.Y; sx *= sample.ScaleX; sy *= sample.ScaleY; angle += sample.Angle; alpha *= sample.Alpha;
                        if (_semanticColors) color = Color.Lerp(color, _palette.Accent(SemanticStyle.Evidence), sample.Color);
                    }
                }
                color.a = alpha; Vector3 center = (baseline[vertex] + baseline[vertex + 2]) * 0.5f;
                float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector3 point = baseline[vertex + corner] - center; float px = point.x * sx, py = point.y * sy;
                    info.meshInfo[mesh].vertices[vertex + corner] = center + new Vector3(px * cosine - py * sine + x, px * sine + py * cosine + y, point.z);
                    info.meshInfo[mesh].colors32[vertex + corner] = color;
                }
            }
        }
        private void OnDestroy() { if (_label != null) _label.OnPreRenderText -= Capture; }
    }
}
