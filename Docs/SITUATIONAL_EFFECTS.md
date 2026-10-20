# Situation colours and screen effects

Based on current main at `c77a7b3`, including merged PR 27. This adds presentation
feedback without changing game decisions, seeds, dialogue or phase rules.

## Visual vocabulary

| Situation | Colour family | Treatment |
| --- | --- | --- |
| Accusation / nomination | Burnt orange by day, warm orange at night | Brief border emphasis, peripheral glow and restrained text shake |
| Refutation | Orange | Slightly stronger response pulse |
| Defense / vouch / acquittal / protection / narrow escape | Teal | Soft reassuring wash and edge glow |
| Agreement | Teal | Short, light border/glow; no full-screen wash |
| Disagreement | Lavender | Brief questioning accent |
| Role claim / reveal / investigation / action report | Blue | Evidence-coloured framing and soft tint |
| Counterclaim / changed claim / blocked ability | Violet | Stronger challenge framing; this does not identify a liar |
| Ballot / tally / trial | Amber | Short ballot accents, stronger tally/trial emphasis |
| Tie / fear / final three / draw | Lavender | Uncertainty pulse |
| Guilt / last words / death / execution / defeat | Rose and crimson | Darkened edges and muted wash; the largest events retain optional limited flash |
| Victory | Lime and gold-green | Longer celebratory glow with no flash or shake |
| Dusk | Indigo | Slow night wash |
| Dawn | Honey gold | Slow warm wash |

Colours are chosen from already-scoped public observations. A false claim and a
true claim receive the same claim treatment. Violet means a public challenge,
not proof of deception. Dialogue and event labels still communicate the action,
so colour is supplementary information.

## Implementation and authoring

`PresentationCue` and `EffectChannel` append values, preserving existing numeric
identities. New `EffectTone` values represent situations; `TextPalette` owns
the actual Unity colours, with different day/night accents. Rules remain unaware
of colour and rendering.

`DefaultCueProfiles` supplies the baseline timelines. Two new bounded channels
are available alongside Border, Vignette, TextShake and Flash:

- **ColorWash:** a low-opacity full-screen tint, capped at 7% before profile and
  user intensity scaling; reduced motion caps it at 3.5%.
- **EdgeGlow:** eight feathered peripheral bands, capped at 30% opacity and
  disabled under reduced motion. No camera displacement or travelling scan lines.

The overlay draws the tint, dark vignette, accent glow, thin border and optional
flash. The vignette now darkens even on the night palette; previously it used
light foreground ink at night. The existing uGUI overlay remains non-interactive
and mesh updates are skipped when its visible values and colour have not changed.
No shaders, image downloads, materials or external packages were added.

Select `Assets/_Project/ScriptableObjects/Effects/StarterEffects.asset` in Unity.
Its Profiles list overrides individual cues. Add tracks for **ColorWash** and
**EdgeGlow** and adjust duration, delay, amount, envelope and blend. Enable
**Override Tone** to select a colour family. Without it, existing overrides
inherit the default cue tone. The existing Dusk override retains its border
track and now includes the new wash/glow. The asset and its GUID are preserved.

**Tools > SocialStim > Effects Playground** now previews situation colours,
washes and glows. Its optional Profile Overrides object field accepts the actual
cue profile asset, so it can preview authored overrides as well as defaults.
The preview is approximate IMGUI geometry; verify the full-screen uGUI result
and text rendering in Play Mode.

Protection now has its own cue. It no longer guesses resource depletion from the
player's current charge count, which could describe someone else's protection.
Ballots and tallies receive distinct cues without changing the vote results.

## Accessibility and display lifetime

Effects Off suppresses all effect channels. Intensity scales the new channels.
Reduced Motion suppresses edge glow, text shake and flash, retaining a gentler
wash and the existing non-moving border/vignette cue. Ordinary speech remains
unaccented unless it has a specific action intent.

Default pulses end within 0.55–1.8 seconds. Default text shake lasts at most
1.5 seconds, and the existing TMP adapter's 1.5-second limit remains in place.
Skip, cancel, completion and restart clear transient washes/glows. Changing
settings mid-line reconfigures the active effect without acknowledging another
line. The existing flash opt-in, low opacity and two-second inter-line cooldown
remain; suppressing a flash does not accidentally suppress wash/glow.

## Verification

- 223 shared portable C# tests passed with NUnit 3.14 / .NET 8.
- NUnit 3.5 / net472 compatibility compilation passed with zero warnings/errors.
- Python scaffold checks and all four validator regression tests passed.
- `git diff --check` passed. Existing seeded AI benchmark outcomes remained identical.

New tests cover intent/event routing, tone selection, invalid tones, bounded
opacities, reduced motion, live settings, flash cooldown isolation, pulse expiry,
skip/cancel cleanup and exactly-once acknowledgement.

Unity 6000.3.26f1 is configured with uGUI 2.0.0. Unity is unavailable here:
adapter compilation, asset import, editor/player test execution, screenshots,
player builds and native profiling were not performed. No removed assets or
serialized fields; new assets include metadata; READMEs untouched.

## Unity checklist

1. Preview accusation, defense, claim, counterclaim, tally, death and victory in
   Effects Playground under both palettes, then repeat using StarterEffects.
2. Play a day/night cycle. Check the coloured border and soft wash settle without
   hiding the dialogue, claims or roster. Check large and small window sizes.
3. Toggle Reduced Motion, Effects Off and intensity while a cue is active. Check
   suppression is immediate and the current text/choices are not skipped.
4. Skip an active death/counterclaim cue, restart, then spectate. Check no glow
   remains stuck and overlays do not intercept mouse or keyboard input.
5. Enable flashes only for the opt-in check. Trigger nearby events and verify
   the cooldown; verify ordinary agreements and ballots never flash.
