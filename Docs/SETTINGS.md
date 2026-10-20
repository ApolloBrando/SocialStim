# Settings overhaul

Preferences now use version 2 JSON in Unity's persistent data directory. The
portable SettingsStore writes a flushed sibling temporary file and atomically
replaces the prior file. Loading malformed/future-version files returns defaults
and a warning without overwriting the file. Version 1 JSON and the previous
PlayerPrefs display preferences migrate with field-level defaults/clamping.
No current-run phase, roles, commands, transcript or beliefs are saved.

The modal groups Game, Text & Display, Accessibility and Data. Display settings
apply immediately. Game settings apply on the next run: population (5–20), rounds,
windows, vote policy, abstention, one tie revote, death reveal, last words, Trial
budget and seed. These implemented settings now replace GameBootstrap's inspector
configuration; old serialized scene fields are ignored and can be removed by the
editor on its next save. The committed scene and all GUIDs remain untouched.

Text/display includes speed (1–240 and Instant), font size, UI scale, auto-advance
hold and Quick phrasing. The latter two are consumed by the subsequent interaction
UI slice. Accessibility includes intensity, reduced motion, flashes, effects and
neutral accents. Each control has Reset; Data resets all defaults. Changes persist
immediately, and Close/Escape closes the panel without a hidden apply/cancel step.
There are no audio controls because no audio system exists. Role presets, role
count/slot editing, stats and difficulty remain with the still-unimplemented role
customization and reasoning phases; no inactive settings are presented for them.

Live playback policy changes preserve the current reveal position and no longer
skip the pending transcript batch. Input remains subject to display acknowledgement.

Manual checks (pending Unity 6000.3.26f1):

- [ ] Import packages/TMP and run EditMode/PlayMode tests, including settings UI.
- [ ] Open each group at 1280x800 and Full HD; scroll, drag sliders, tab/navigate,
      press Escape and verify keyboard focus stays inside/returns after closing.
- [ ] Change font size/scale/speed during a line; no unrelated line is skipped.
- [ ] Toggle reduced motion/flashes/effects and reset individual values.
- [ ] Change game settings, start a new run, and confirm cast/round/vote/seed values.
- [ ] Restart Play Mode: all choices persist. Corrupt one numeric setting: only
      that field clamps. Malformed/future-version files show a load warning.
- [ ] Verify old PlayerPrefs migrate once when no JSON exists. Reset all restores
      speed 84, reduced motion on, flashes off, and five-round Gnosia mode.

Local scaffold validation passes. GitHub Actions tests real serialization,
per-field clamping, v1 migration, future-file preservation, atomic replacement,
per-setting reset and live reveal policy. Unity input/rendering/builds are unrun.
