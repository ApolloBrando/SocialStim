using System;
using System.Collections.Generic;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using TMPro;
using UnityEngine;

namespace SocialStim.Presentation.Council
{
    /// <summary>Manual line reveal/advance adapter; it never submits a game command.</summary>
    internal sealed class CouncilDialoguePlayer
    {
        private readonly DisplayPlayback _playback = new DisplayPlayback();
        private readonly TextEffectRenderer _renderer;
        private readonly TextMeshProUGUI _label;
        private readonly IReadOnlyDictionary<PresentationCue, EffectProfile> _profiles;
        private IReadOnlyList<CouncilEvent> _events = Array.Empty<CouncilEvent>();
        private TextPalette _palette;
        private EffectsSettings _settings;
        private int _cursor;
        private bool _started;
        private int _lastPressFrame = -1;
        internal CouncilDialoguePlayer(TextEffectRenderer renderer, TextMeshProUGUI label, EffectsSettings settings,
            IReadOnlyDictionary<PresentationCue, EffectProfile> profiles)
        { _renderer = renderer; _label = label; _settings = settings; _profiles = profiles; _renderer.Initialize(label); _renderer.SetSettings(settings); }
        internal event Action<CouncilEvent> Started;
        internal event Action<CouncilEvent> Delivered;
        internal event Action Completed;
        internal bool Pending => _cursor < _events.Count;
        internal bool Revealing => _playback.Busy;
        internal CouncilEvent Current => Pending ? _events[_cursor] : null;
        internal void Render(IReadOnlyList<CouncilEvent> events, TextPalette palette)
        { _events = events; _palette = palette; if (Pending && !_started) Start(); }
        internal void Reset()
        { _playback.Cancel(); _events = Array.Empty<CouncilEvent>(); _cursor = 0; _started = false; _label.text = ""; }
        internal void Tick(float seconds)
        {
            if (Pending) _playback.Tick(seconds);
            _renderer.Apply(_playback.VisibleUtf16, _playback.Frame, Time.unscaledTime);
        }
        internal void Advance()
        {
            if (!Pending || _lastPressFrame == Time.frameCount) return;
            _lastPressFrame = Time.frameCount;
            if (_playback.Busy) { _playback.Skip(); return; }
            Delivered?.Invoke(_events[_cursor]); _cursor++; _started = false;
            if (Pending) Start(); else Completed?.Invoke();
        }
        internal void Settings(EffectsSettings settings)
        { _settings = settings; _renderer.SetSettings(settings); _playback.Reconfigure(settings); }
        internal void Palette(TextPalette palette) { _palette = palette; _renderer.SetPalette(palette); }
        private void Start()
        {
            _started = true; var record = _events[_cursor]; Started?.Invoke(record);
            SemanticText text;
            try { text = SemanticMarkup.Parse(record.Text); } catch (ArgumentException) { text = SemanticText.Literal(record.Text); }
            _renderer.SetDocument(text, 0, _palette);
            PresentationCue cue = record.Intent == SpeechIntent.Accuse ? PresentationCue.Accusation
                : record.Intent == SpeechIntent.Defend || record.Intent == SpeechIntent.Vouch ? PresentationCue.Defense
                : record.Kind == CouncilEventKind.Death ? PresentationCue.Death : record.Kind == CouncilEventKind.GameOver ? PresentationCue.Victory : PresentationCue.Speech;
            _playback.Begin(record.Sequence, text, _profiles[cue], _settings, null);
        }
    }
}
