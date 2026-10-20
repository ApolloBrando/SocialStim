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
        private float _speedMultiplier;
        private float _visibleElements;
        private float _pauseAfter;
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
            _speedMultiplier = speedMultiplier; _pauseAfter = pauseAfter; _visibleElements = 0;
            _speed = settings.ElementsPerSecond * speedMultiplier;
            _duration = !settings.Enabled ? 0 : Math.Max(settings.InstantText ? 0 : text.ElementCount / _speed, profile.Duration)
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
            _visibleElements = Math.Min(Text.ElementCount, _visibleElements + seconds * _speed);
            VisibleUtf16 = !_settings.Enabled || _settings.InstantText ? Text.Text.Length
                : Math.Max(VisibleUtf16, Text.EndOfElements((int)_visibleElements));
            Frame = _profile.Sample(_elapsed, _settings);
            if (!_permitFlash) Frame = new EffectFrame(Frame.Shake, Frame.Border, Frame.Vignette, 0);
            else if (Frame.Flash > 0 && !_flashStarted) { _nextFlash = _clock + 2; _flashStarted = true; }
            if (_elapsed >= _duration) Finish(DisplayCompletion.Completed);
        }
        /// <summary>Applies accessibility/speed without skipping queued observations or restarting the line.</summary>
        public void UpdateSettings(EffectsSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!_active || Text == null) return;
            _speed = settings.ElementsPerSecond * _speedMultiplier;
            if (settings.InstantText || !settings.Enabled)
            { _visibleElements = Text.ElementCount; VisibleUtf16 = Text.Text.Length; }
            if (Busy)
                _duration = Math.Max(_profile.Duration, _elapsed + (Text.ElementCount - _visibleElements) / _speed
                    + (settings.InstantText ? 0 : _pauseAfter));
            EffectFrame sample = _profile.Sample(_elapsed, settings);
            Frame = new EffectFrame(sample.Shake, sample.Border, sample.Vignette, 0);
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
