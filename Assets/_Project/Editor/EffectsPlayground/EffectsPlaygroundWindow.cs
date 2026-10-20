using System;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor.EffectsPlayground
{
    /// <summary>Editor-only portable playback preview; does not create or modify a scene.</summary>
    public sealed class EffectsPlaygroundWindow : EditorWindow
    {
        private readonly DisplayPlayback _playback = new DisplayPlayback();
        private PresentationCue _cue = PresentationCue.Accusation;
        private string _source = "<emphasis>I heard you.</emphasis> <fear>Something is wrong.</fear>";
        private bool _night, _reduced = true, _flash, _instant;
        private float _intensity = 0.5f;
        private string _result = "No display started.";
        private double _last;
        private int _sequence;
        private GUIStyle _style;
        private CueProfileDefinition _definitions;
        private EffectTone _tone;
        [MenuItem("Tools/SocialStim/Effects Playground")]
        public static void Open() => GetWindow<EffectsPlaygroundWindow>("SocialStim Effects");
        private void OnEnable()
        {
            _last = EditorApplication.timeSinceStartup;
            _style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false, fontSize = 18 };
        }
        private void OnGUI()
        {
            _cue = (PresentationCue)EditorGUILayout.EnumPopup("Visible cue", _cue);
            _definitions = (CueProfileDefinition)EditorGUILayout.ObjectField("Profile overrides", _definitions, typeof(CueProfileDefinition), false);
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
            var palette = new TextPalette(_night);
            Color background = palette.Background;
            Color ink = palette.Foreground;
            Color accent = palette.Situation(_tone);
            EditorGUI.DrawRect(rect, background);
            EffectFrame frame = _playback.Frame;
            Color wash = accent; wash.a = frame.ColorWash; EditorGUI.DrawRect(rect, wash);
            Color edge = Color.Lerp(background, accent, frame.Border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), edge);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), edge);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), edge);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), edge);
            for (int i = 0; i < 8; i++)
            {
                Color vignette = Color.Lerp(Color.black, accent, 0.15f); vignette.a = frame.Vignette * 0.3f * (1 - i / 8f);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.y + i * 2, rect.width - i * 4, 2), vignette);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.yMax - 2 - i * 2, rect.width - i * 4, 2), vignette);
                Color glow = accent; glow.a = frame.EdgeGlow * (1 - i / 8f);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.y + i * 2, rect.width - i * 4, 2), glow);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.yMax - 2 - i * 2, rect.width - i * 4, 2), glow);
                EditorGUI.DrawRect(new Rect(rect.x + i * 2, rect.y + i * 2, 2, rect.height - i * 4), glow);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2 - i * 2, rect.y + i * 2, 2, rect.height - i * 4), glow);
            }
            string text = _playback.Text == null ? "Preview" : _playback.Text.Text.Substring(0, _playback.VisibleUtf16);
            _style.normal.textColor = ink;
            float shake = Mathf.Sin(_playback.Elapsed * 27) * frame.Shake * 3;
            GUI.Label(new Rect(rect.x + 16 + shake, rect.y + 16, rect.width - 32, rect.height - 32), text, _style);
            Color flash = accent; flash.a = frame.Flash; EditorGUI.DrawRect(rect, flash);
            EditorGUILayout.LabelField(_result);
            EditorGUILayout.HelpBox("Preview uses the selected overrides or defaults. Check full-screen geometry and TMP rendering in Play Mode.", MessageType.Info);
        }
        private void Play()
        {
            _playback.Cancel();
            try
            {
                var text = SemanticMarkup.Parse(_source);
                EffectProfile profile = (_definitions == null ? DefaultCueProfiles.Create() : _definitions.Load())[_cue];
                _tone = profile.Tone;
                _playback.Begin(++_sequence, text, profile,
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
