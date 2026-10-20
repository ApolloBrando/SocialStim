# Settings and new-run customization

Modern council play uses grouped native uGUI sliders, toggles and buttons in the single drawer. No audio page appears because no audio system exists. The optional legacy trial view retains its existing preferences adapter.

## Behavior

- Text & Display: live reveal speed (default **84** text elements/second; previous default **28**), Instant, optional length-scaled auto-advance, dialogue text size, UI scale, Quick phrasing.
- Accessibility: effects, intensity, reduced motion, limited flashes, colorblind accents. Reduced motion and colorblind mode default on; flashes default off. Labels identify claims and state independently of color.
- Game: explicit draft, Apply/Cancel; Basic, per-role minimum/maximum (maximum zero disables), exact/random faction slots, Rules, Stats and Seed pages.
- Start/Apply appears only when the draft validates. Corrections restore it without reopening the page. Every individual control has a reset; Data offers Reset All.
- Game changes affect new runs. Continue resumes only the current in-memory run.
- The six point-buy stats must total exactly 30; difficulty/stat behavior is connected in the AI phase on its stacked branch.
- Day discussion/voting and night actions are the only gameplay periods; there is no additional activity setting.

Preferences use version 2 JSON at `Application.persistentDataPath/settings-v2.json`. The file holds display and setup preferences only: no cast assignment, transcript, resource state, relationship or ongoing run. Values clamp individually; malformed data recovers to defaults with a warning. Version 0/1 JSON and the existing PlayerPrefs UI format migrate; an old default speed of 28 becomes 84, custom speeds survive. Unknown future versions load defaults with a warning. Explicit preference changes may replace that file.

Display edits are debounced by 0.35 seconds, with a final flush on component destruction. Game Apply saves explicitly; Cancel restores the draft. A save failure leaves preferences dirty, reports a short warning, and retries later. File replacement uses a temporary file; the platform fallback copies only if File.Replace is unsupported.

## Code

- `Application/Settings/`: immutable preferences, codec/migration, file port, validation and draft service.
- `Presentation/Council/CouncilSettingsPanel.cs`: native controls only.
- `Presentation/Text/GameBootstrap.cs`: persistent path, migration, wiring and flush.
- `Presentation/Council/CouncilDialoguePlayer.cs`: reveal/advance display adapter.
- `Presentation/Council/CouncilLayout.cs`: responsive layout and scale.

The old redundant CouncilScreen.ShowSetup role picker was removed after bootstrap switched to the complete editor. No files or GUIDs were deleted.

## Unity checklist (not executed here)

1. Start Game: Main Menu offers New Run / Settings / Quit; Continue appears only after starting a run.
2. Open every settings group with mouse and keyboard; verify each toggle reflects loaded data, values update while dragging, and focus remains visible.
3. Change speed while a line is partway revealed. Its position must not jump or acknowledge another line. First Space/click completes it; second advances.
4. Enable auto-advance; short/long lines get the configured delay plus a bounded reading delay. Opening the drawer pauses display playback.
5. Change text size and UI scale at 1280×720, 1920×1080, and a narrow window. Check roster scrolling, line bounds, drawer scrolling, and control focus.
6. Try Classic / Chaos / Minimal / Custom; disable Archon, oversubscribe Evil, set contradictory limits/slots. Start must disappear with a short reason, then return after correction.
7. Select a role/faction, assign slots, seed a run, and verify the chosen role/faction and exact counts. Repeat identical seed/settings/inputs.
8. Edit game settings then Cancel: the running game and saved setup must remain unchanged. Apply must affect only the next run.
9. Test every reset; test point-buy totals 29/30/31; copy a seed from My Role.
10. Restart the editor/player: settings survive; the run does not. Corrupt JSON, try version 1 and a future version, and check recovery/warning.
11. Toggle reduced motion, effects, flashes and colorblind mode during playback. Verify readable contrast, non-color claim/state labels and safety-limited flashes.
12. Test legacy Trial Mode separately; its inspector fields and asset GUIDs are unchanged.
