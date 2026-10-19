# Unity verification gate: milestone 05

All boxes remain pending until executed in Unity 6000.3.26f1. This is a separate
branch from the owner's main-branch test; do not overwrite that checkout.

- [ ] Open `milestone/05-presentation-effects` in a separate checkout. Resolve
  packages and TMP Essential Resources as in `UNITY_VERIFICATION.md`.
- [ ] Confirm no compile errors in runtime, editor or test assemblies. The
  PlayMode test assembly references uGUI/TMP for its actual adapter checks.
- [ ] Run Tools > SocialStim > Validate Starter Content. Expect 5 roles, 45
  placeholder lines and 20 names; malformed semantic markup must fail clearly.
- [ ] Inspect Game.unity: StarterCatalog and StarterEffects assigned; no manual
  camera, material, URP or shader setup needed.
- [ ] Run EditMode and PlayMode suites. TMP adapter tests require imported fonts;
  presenter blocking is also tested in the independent headless project.
- [ ] Confirm day is white text on near-black and night is black text on soft
  white, including status, buttons and footer. Check a complete dusk/dawn cycle.
- [ ] Test each semantic style and nested tags on both backgrounds. No literal
  TMP/HTML execution or sprite/material injection; all facts remain in words.
- [ ] Check glyph reveal including surrogate pairs and combining marks. Skip or
  Space must reveal the whole pending batch without advancing a game command.
- [ ] While a batch displays, Enter/mouse/gamepad cannot submit a role action.
  Choices unlock afterward. New run while displaying must cancel and clear effects.
- [ ] Navigate every long choice list with arrows. The selected `>` button stays
  visible. Scroll both panels at 1280x800 and at a smaller desktop resolution.
- [ ] Exercise effects off, reduced motion, instant text and all intensity values.
  Effects off is immediate neutral text. Reduced motion suppresses shake/flash.
- [ ] Opt into flash with full motion. Check one gentle pulse per profile, at
  most 8% opacity, with a two-second start cooldown. Restore flashes off afterward.
- [ ] Inspect border and vignette without blocking pointer input. Disable/reset
  during shake; verify cached glyph positions and opacity return to baseline.
- [ ] Change an inspector profile (duration, envelope, overlap, priority and blend)
  and restart Play Mode. Overrides affect presentation only; the same seed and
  player inputs must retain the same gameplay transcript and result.
- [ ] Open Effects Playground outside Play Mode. Check play, skip, cancel,
  malformed tags, both palettes and accessibility controls. It previews default
  portable profiles; confirm actual asset overrides in the scene separately.
- [ ] Warm up a long dialogue, then profile steady Tick, glyph Apply and overlay
  updates with the Unity CPU Profiler and GC Alloc column. Require zero managed
  allocations in steady effect updates. Record text/layout/font atlas rebuilds
  and first mesh-capacity growth separately. Do not infer this from .NET tests.
- [ ] Build the enabled Game scene with the existing Mono configuration and
  repeat palette, input, skip/cancel and accessibility checks. IL2CPP remains pending.
- [ ] Record exact editor version, results, profiler captures and any reproduction
  steps in the draft PR before calling the effects slice Unity-verified.
