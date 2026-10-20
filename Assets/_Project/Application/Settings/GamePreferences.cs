using System;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Application.Settings
{
    /// <summary>Immutable display/accessibility and next-run preferences; contains no current run state.</summary>
    public sealed class GamePreferences
    {
        /// <summary>Creates a validated snapshot; all effects remain safe by default.</summary>
        public GamePreferences(EffectsSettings effects = null, NewRunSettings game = null, float textSize = 22,
            float uiScale = 1, bool quickMode = false, bool autoAdvance = false, float advanceDelay = 1, bool colorblind = false)
        {
            if (!Finite(textSize) || textSize < 16 || textSize > 36 || !Finite(uiScale) || uiScale < 0.75f || uiScale > 1.5f
                || !Finite(advanceDelay) || advanceDelay < 0.25f || advanceDelay > 5) throw new ArgumentOutOfRangeException(nameof(textSize));
            Effects = effects ?? new EffectsSettings(); Game = game ?? new NewRunSettings(); TextSize = textSize;
            UiScale = uiScale; QuickMode = quickMode; AutoAdvance = autoAdvance; AdvanceDelay = advanceDelay; Colorblind = colorblind;
        }
        /// <summary>Text/effect safety policy.</summary>
        public EffectsSettings Effects { get; }
        /// <summary>Settings used only when starting the next run.</summary>
        public NewRunSettings Game { get; }
        /// <summary>Dialogue font size in reference-resolution units.</summary>
        public float TextSize { get; }
        /// <summary>Additional user-selected interface scale.</summary>
        public float UiScale { get; }
        /// <summary>Skips phrasing selection using the first legal line.</summary>
        public bool QuickMode { get; }
        /// <summary>Automatically advances completed lines after a reading delay.</summary>
        public bool AutoAdvance { get; }
        /// <summary>Minimum automatic reading hold in seconds.</summary>
        public float AdvanceDelay { get; }
        /// <summary>Uses one neutral text accent while preserving word/symbol information.</summary>
        public bool Colorblind { get; }
        /// <summary>Copies preferences with a new validated effect policy.</summary>
        public GamePreferences WithEffects(EffectsSettings effects) => new GamePreferences(effects, Game, TextSize, UiScale, QuickMode, AutoAdvance, AdvanceDelay, Colorblind);
        /// <summary>Copies preferences with next-run configuration.</summary>
        public GamePreferences WithGame(NewRunSettings game) => new GamePreferences(Effects, game, TextSize, UiScale, QuickMode, AutoAdvance, AdvanceDelay, Colorblind);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
