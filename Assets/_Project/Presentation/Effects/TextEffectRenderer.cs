using System;
using SocialStim.Application.PresentationCues;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Cached TMP geometry; no strings, collections or materials created during Apply.</summary>
    public sealed class TextEffectRenderer : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private ActionUnderlineGraphic _underlines;
        private SemanticText _document;
        private TextPalette _palette;
        private Vector3[][] _vertices = Array.Empty<Vector3[]>();
        private SemanticStyle[] _styles = Array.Empty<SemanticStyle>();
        private int _visible = int.MaxValue;
        private int _animatedStart;
        private EffectFrame _frame;
        private float _time;
        private float _motionStarted;
        private bool _semanticColors = true;
        private bool _dirty = true;
        private bool _underlineLayoutDirty;
        private bool _hasFear;
        private bool _wasMoving;
        private EffectsSettings _settings = new EffectsSettings();
        public void Initialize(TextMeshProUGUI label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (_label != null) _label.OnPreRenderText -= Capture;
            _label = label; _label.OnPreRenderText += Capture;
            if (_underlines == null)
            {
                var root = new GameObject("Action underlines", typeof(RectTransform), typeof(CanvasRenderer)); root.transform.SetParent(label.transform, false);
                var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                _underlines = root.AddComponent<ActionUnderlineGraphic>(); _underlines.raycastTarget = false;
            }
        }
        public void SetDocument(SemanticText document, int animatedStart, TextPalette palette)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (_label == null) throw new InvalidOperationException("Initialize the text renderer first.");
            _motionStarted = Time.unscaledTime;
            _document = document; _animatedStart = animatedStart; _palette = palette;
            _hasFear = false;
            foreach (SemanticSpan span in document.Spans)
                if (span.Style == SemanticStyle.Fear && span.Start + span.Length > animatedStart) _hasFear = true;
            _dirty = true;
            _visible = animatedStart; _frame = default;
            _label.text = document.Text; _label.richText = false;
            _label.ForceMeshUpdate();
        }
        public void SetPalette(TextPalette palette) { _palette = palette; _dirty = true; }
        /// <summary>Updates accessibility without restarting display or mesh ownership.</summary>
        public void SetSettings(EffectsSettings settings)
        { _settings = settings ?? throw new ArgumentNullException(nameof(settings)); _semanticColors = settings.Enabled; _dirty = true; }
        public void Apply(int visibleUtf16, EffectFrame frame, float time)
        {
            if (_label == null || _document == null) return;
            if (Time.unscaledTime - _motionStarted >= 1.5f) frame = default;
            bool moving = _settings.Enabled && !_settings.ReducedMotion
                && ((frame.Shake > 0 || _hasFear && _settings.Intensity > 0) && Time.unscaledTime - _motionStarted < 1.5f);
            // Static settled text needs no mesh upload every frame. Policy/palette/layout
            // changes invalidate this cache; active motion keeps sampling unscaled time.
            if (!_dirty && _visible == visibleUtf16 && _frame.Shake == frame.Shake && !moving && !_wasMoving) return;
            _wasMoving = moving;
            _visible = visibleUtf16; _frame = frame; _time = time; _dirty = false;
            Paint(_label.textInfo);
            RefreshUnderlines();
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
        private void Capture(TMP_TextInfo info)
        {
            if (_document == null) return;
            _dirty = true;
            _underlineLayoutDirty = true;
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
        private void RefreshUnderlines()
        {
            if (_document == null || _underlines == null) return;
            // TMP invokes Capture during the canvas graphic pass. Enqueue its sibling geometry only after that pass.
            if (CanvasUpdateRegistry.IsRebuildingGraphics()) { _underlineLayoutDirty = true; return; }
            _underlines.Configure(_label, _document, _visible, _palette.Muted);
            if (!_underlineLayoutDirty) return;
            _underlineLayoutDirty = false;
            _underlines.SetVerticesDirty();
        }
        private void LateUpdate() { if (_underlineLayoutDirty) RefreshUnderlines(); }
        private void Paint(TMP_TextInfo info)
        {
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || character.materialReferenceIndex >= _vertices.Length) continue;
                int mesh = character.materialReferenceIndex, vertex = character.vertexIndex;
                Vector3[] baseline = _vertices[mesh];
                if (baseline == null || vertex + 3 >= baseline.Length) continue;
                Color32 color = _palette.Accent(_semanticColors || (int)_styles[i] >= (int)SemanticStyle.Participant ? _styles[i] : SemanticStyle.Default);
                if (character.index >= _visible) color.a = 0;
                float continuous = _settings.Enabled && !_settings.ReducedMotion && _styles[i] == SemanticStyle.Fear && Time.unscaledTime - _motionStarted < 1.5f
                    ? 0.2f * _settings.Intensity : 0;
                float strength = _settings.Enabled && !_settings.ReducedMotion
                    && character.index >= _animatedStart && character.index < _visible
                    ? Math.Max(_frame.Shake, continuous) : 0;
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
