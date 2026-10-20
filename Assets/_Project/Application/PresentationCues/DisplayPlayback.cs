using System;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>One display transaction, with exactly-once acknowledgement and no gameplay clock.</summary>
    public sealed class DisplayPlayback
    {
        private EffectProfile _profile;
        private EffectsSettings _settings;
        private Action<DisplayAcknowledgement> _acknowledge;
        private int _sequence;
        private float _elapsed;
        private float _duration;
        private float _speed;
        private float _revealOffset;
        private float _pauseAfter;
        private float _speedMultiplier;
        private double _clock;
        private double _nextFlash;
        private bool _permitFlash;
        private bool _flashStarted;
        private bool _active;
        public bool Busy { get; private set; }
        public SemanticText Text { get; private set; }
        public int VisibleUtf16 { get; private set; }
        public float Elapsed => _elapsed;
        public EffectFrame Frame { get; private set; }

        public void Begin(int sequence, SemanticText text, EffectProfile profile, EffectsSettings settings, Action<DisplayAcknowledgement> acknowledge,
            float pauseAfter = 0, float speedMultiplier = 1)
        {
            if (!EffectStep.Finite(pauseAfter) || pauseAfter < 0 || pauseAfter > 10
                || !EffectStep.Finite(speedMultiplier) || speedMultiplier < 0.25f || speedMultiplier > 4)
                throw new ArgumentOutOfRangeException(nameof(pauseAfter));
            if (Busy) throw new InvalidOperationException("Cancel or complete the active display first.");
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sequence = sequence; _acknowledge = acknowledge; _elapsed = 0; Frame = default; VisibleUtf16 = 0; Busy = true; _active = true;
            _permitFlash = _clock >= _nextFlash; _flashStarted = false;
            _speedMultiplier = speedMultiplier; _pauseAfter = pauseAfter; _revealOffset = 0;
            _speed = settings.ElementsPerSecond * speedMultiplier;
            _duration = !settings.Enabled ? 0 : Math.Max(settings.InstantText || text.ElementCount == 0 ? 0 : _speed == 0 ? float.PositiveInfinity : text.ElementCount / _speed, profile.Duration)
                + (settings.InstantText ? 0 : pauseAfter);
            if (!settings.Enabled || settings.InstantText) VisibleUtf16 = text.Text.Length;
            if (_duration == 0) Finish(DisplayCompletion.Completed);
        }
        public void Tick(float seconds)
        {
            if (!EffectStep.Finite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            _clock += seconds;
            if (!_active) return;
            _elapsed += seconds;
            if (!Busy)
            {
                EffectFrame retained = _profile.Sample(_elapsed, _settings);
                Frame = new EffectFrame(retained.Shake, 0, 0, 0);
                return;
            }
            VisibleUtf16 = !_settings.Enabled || _settings.InstantText ? Text.Text.Length
                : Text.EndOfElements((int)Math.Min(Text.ElementCount, _elapsed * _speed + _revealOffset));
            Frame = _profile.Sample(_elapsed, _settings);
            if (!_permitFlash) Frame = new EffectFrame(Frame.Shake, Frame.Border, Frame.Vignette, 0, Frame.ColorWash, Frame.EdgeGlow);
            else if (Frame.Flash > 0 && !_flashStarted) { _nextFlash = _clock + 2; _flashStarted = true; }
            if (_elapsed >= _duration) Finish(DisplayCompletion.Completed);
        }
        /// <summary>Applies display preferences without skipping this line or acknowledging queued lines.</summary>
        public void Reconfigure(EffectsSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            float revealed = Text == null ? 0 : !_settings.Enabled || _settings.InstantText ? Text.ElementCount
                : Math.Min(Text.ElementCount, _elapsed * _speed + _revealOffset);
            _settings = settings;
            if (!_active) return;
            if (!Busy)
            {
                // Settled continuous effects still obey newly changed accessibility settings.
                EffectFrame retained = _profile.Sample(_elapsed, settings);
                Frame = new EffectFrame(retained.Shake, 0, 0, 0);
                return;
            }
            _speed = settings.ElementsPerSecond * _speedMultiplier;
            _revealOffset = revealed - _elapsed * _speed;
            if (!settings.Enabled || settings.InstantText)
            {
                VisibleUtf16 = Text.Text.Length;
                _duration = settings.Enabled ? Math.Max(_elapsed, _profile.Duration) : _elapsed;
            }
            else if (revealed < Text.ElementCount)
                _duration = Math.Max(_profile.Duration, _speed == 0 ? float.PositiveInfinity : _elapsed + (Text.ElementCount - revealed) / _speed) + _pauseAfter;
            else _duration = Math.Max(_elapsed, _profile.Duration) + _pauseAfter;
            Frame = _profile.Sample(_elapsed, settings);
            if (!_permitFlash) Frame = new EffectFrame(Frame.Shake, Frame.Border, Frame.Vignette, 0, Frame.ColorWash, Frame.EdgeGlow);
        }

        public void Skip() { if (Busy) Finish(DisplayCompletion.Skipped); }
        public void Cancel()
        {
            _active = false; Frame = default;
            if (Busy) Finish(DisplayCompletion.Cancelled);
        }
        private void Finish(DisplayCompletion completion)
        {
            Busy = false; VisibleUtf16 = Text.Text.Length;
            if (completion == DisplayCompletion.Cancelled) _active = false;
            EffectFrame retained = _active ? _profile.Sample(_elapsed, _settings) : default;
            Frame = new EffectFrame(retained.Shake, 0, 0, 0);
            Action<DisplayAcknowledgement> callback = _acknowledge;
            _acknowledge = null;
            callback?.Invoke(new DisplayAcknowledgement(_sequence, completion));
        }
    }
}
