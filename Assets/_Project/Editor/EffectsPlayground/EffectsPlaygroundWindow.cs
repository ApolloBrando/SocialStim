using System;
using SocialStim.Application.PresentationCues;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor.EffectsPlayground
{
    /// <summary>Editor-only portable playback preview; does not create or modify a scene.</summary>
    public sealed class EffectsPlaygroundWindow : EditorWindow
    {
        private readonly DisplayPlayback _playback = new DisplayPlayback();
        private GlyphEffectKind _glyphEffect = GlyphEffectKind.Wave;
        private EffectLifetime _lifetime = EffectLifetime.Continuous;
        private float _speed = 1, _amplitude = 3, _duration = 1;
        private PresentationCue _cue = PresentationCue.Accusation;
        private string _source = "<emphasis>I heard you.</emphasis> <fear>Something is wrong.</fear>";
        private bool _night, _reduced = true, _flash, _instant;
        private float _intensity = 0.5f;
        private string _result = "No display started.";
        private double _last;
        private int _sequence;
        private GUIStyle _style;
        [MenuItem("Tools/SocialStim/Effects Playground")]
        public static void Open() => GetWindow<EffectsPlaygroundWindow>("SocialStim Effects");
        private void OnEnable()
        {
            _last = EditorApplication.timeSinceStartup;
            _style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false, fontSize = 18 };
        }
        private void OnGUI()
        {
            _glyphEffect = (GlyphEffectKind)EditorGUILayout.EnumPopup("Glyph effect (32)", _glyphEffect);
            _lifetime = (EffectLifetime)EditorGUILayout.EnumPopup("Span lifetime", _lifetime);
            _speed = EditorGUILayout.Slider("Glyph speed", _speed, 0.1f, 20);
            _amplitude = EditorGUILayout.Slider("Glyph amplitude", _amplitude, 0, 20);
            _duration = EditorGUILayout.Slider("Span duration", _duration, 0.05f, 10);
            _cue = (PresentationCue)EditorGUILayout.EnumPopup("Visible cue", _cue);
            _source = EditorGUILayout.TextArea(_source, GUILayout.Height(72));
            _night = EditorGUILayout.Toggle("Night palette", _night);
            _reduced = EditorGUILayout.Toggle("Reduced motion", _reduced);
            _flash = EditorGUILayout.Toggle("Allow limited flash", _flash);
            _instant = EditorGUILayout.Toggle("Instant text", _instant);
            _intensity = EditorGUILayout.Slider("Intensity", _intensity, 0, 1);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play")) Play();
            if (GUILayout.Button("Skip")) _playback.Skip();
            if (GUILayout.Button("Cancel")) _playback.Cancel();
            EditorGUILayout.EndHorizontal();
            Rect rect = GUILayoutUtility.GetRect(100, 220, GUILayout.ExpandWidth(true));
            Color background = _night ? new Color(0.075f, 0.09f, 0.105f) : new Color(0.975f, 0.955f, 0.87f);
            Color ink = _night ? new Color(0.94f, 0.92f, 0.86f) : new Color(0.16f, 0.14f, 0.13f);
            EditorGUI.DrawRect(rect, background);
            EffectFrame frame = _playback.Frame;
            Color edge = Color.Lerp(background, ink, frame.Border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), edge);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), edge);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), edge);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), edge);
            for (int i = 0; i < 8; i++)
            {
                Color vignette = ink; vignette.a = frame.Vignette * 0.15f * (1 - i / 8f);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.y + i * 2, rect.width - i * 4, 2), vignette);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.yMax - 2 - i * 2, rect.width - i * 4, 2), vignette);
            }
            string text = _playback.Text == null ? "Preview" : _playback.Text.Text.Substring(0, _playback.VisibleUtf16);
            _style.normal.textColor = ink;
            float x = rect.x + 16, y = rect.y + 16;
            var policy = new EffectsSettings(reducedMotion: _reduced, flashes: _flash, intensity: _intensity);
            var effect = new GlyphEffect(_glyphEffect, speed: _speed, amplitude: _amplitude, duration: _duration, lifetime: _lifetime);
            Matrix4x4 original = GUI.matrix; Color oldColor = GUI.color;
            for (int i = 0; i < text.Length; i++)
            {
                string glyph = text[i].ToString(); float width = Mathf.Max(4, _style.CalcSize(new GUIContent(glyph)).x);
                if (x + width > rect.xMax - 16 || glyph == "\n") { x = rect.x + 16; y += 26; }
                if (glyph == "\n") continue;
                GlyphTransform sample = GlyphEffectSampler.Sample(effect, _playback.Elapsed, i, policy, _playback.Elapsed);
                Vector3 center = new Vector3(x + width / 2, y + 13, 0);
                GUI.matrix = original * Matrix4x4.TRS(center + new Vector3(sample.X, sample.Y, 0),
                    Quaternion.Euler(0, 0, sample.Angle * Mathf.Rad2Deg), new Vector3(sample.ScaleX, sample.ScaleY, 1))
                    * Matrix4x4.Translate(-center);
                GUI.color = new Color(1, 1, 1, sample.Alpha); GUI.Label(new Rect(x, y, width + 4, 28), glyph, _style); x += width;
            }
            GUI.matrix = original; GUI.color = oldColor;
            Color flash = ink; flash.a = frame.Flash; EditorGUI.DrawRect(rect, flash);
            EditorGUILayout.LabelField(_result);
            EditorGUILayout.HelpBox("Default profiles. Check scene overrides and TMP rendering in Play Mode.", MessageType.Info);
        }
        private void Play()
        {
            _playback.Cancel();
            try
            {
                var text = SemanticMarkup.Parse(_source);
                _playback.Begin(++_sequence, text, DefaultCueProfiles.Create()[_cue],
                    new EffectsSettings(reducedMotion: _reduced, flashes: _flash, intensity: _intensity, instantText: _instant),
                    acknowledgement => _result = $"Display {acknowledgement.Sequence}: {acknowledgement.Completion}");
            }
            catch (ArgumentException exception) { _result = exception.Message; }
            _last = EditorApplication.timeSinceStartup; Repaint();
        }
        private void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            bool wasBusy = _playback.Busy;
            _playback.Tick((float)Math.Min(0.1, Math.Max(0, now - _last))); _last = now;
            if (wasBusy || _playback.Busy || _playback.Text != null) Repaint();
        }
        private void OnDisable() { _playback.Cancel(); }
    }
}
