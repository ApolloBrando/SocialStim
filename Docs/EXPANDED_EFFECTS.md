# Expanded span effects

The catalog implements 32 glyph behaviors: Shake, Tremble, Quake, Jitter, Wave,
Wobble, Bounce, Float, Sway, Pulse, Heartbeat, Breathe, Flicker, Blink, FadeIn,
FadeOut, Glitch, Noise, Stutter, Whisper, Shout, Drip, Melt, Crumble, Rise,
Ripple, Zoom, Squash, Gradient, ColorPulse, Spotlight and Strobe.

These are vertex/scale/rotation/opacity/contrast-preserving accent effects.
This does not claim texture-based echo, strikethrough geometry or true glyph
scramble/decryption, which require additional rendering work.

Semantic recipes: emphasis, fear, calm, evidence, danger, rage, whisper, shout,
lie-tell, dread and glitch. Optional TextStyleDefinition assets override up to
four effects per known semantic tag; assign one to GameBootstrap Text Styles.
No asset edits or new packages are required for defaults. Rules remain unaware
of markup. Style names never prove that a character is lying.

Explicit authoring examples:

- `<wave>continuous motion</wave>`
- `<shake i=2 d=3>three seconds</shake>`
- `<bounce s=2 a=4 d=2 mode=envelope>attack, sustain, release</bounce>`
- `<fear>nested <glitch>independent effects</glitch></fear>`

Parameters: intensity i 0–4, speed s 0.1–20, amplitude a 0–20 reference pixels,
duration d 0.05–30 seconds, mode Continuous/Timed/Burst/Envelope. Explicit d
without mode means Timed. Burst decays; Envelope ramps in/out. Continuous spans
last from each glyph's reveal until replacement/dismissal, independently of the
typewriter. Nested ranges stack. Unknown/duplicate parameters, unknown tags,
bad nesting and non-finite numbers fail parsing. Limits: eight nested tags,
64 effect ranges and 2048 displayed UTF-16 units per line.

Glyph movement uses per-character phases. Flash-like alpha effects share a clock,
ignore unsafe authored rates and stay at/below 1.5 pulses per second. Flashes off
removes their modulation. Reduced motion removes position/scale/rotation and
uses restrained accents where appropriate. Defaults remain reduced motion and
flashes off. Screen pulse safety from the previous slice remains unchanged.

Only the current line is meshed; history is retained separately in the information
drawer. The adapter caps actual span samples at 16,384 per frame, leaving excess
glyphs static for that frame. Cached baselines prevent transform drift. Parsing and buffer growth
are document/layout work; Apply/Sample create no managed collections or strings.
Portable sampling has an allocation regression; real TMP uploads/GC/frame cost
still require Unity profiling. Effects Playground now offers every catalog entry,
lifetime, speed, amplitude and intensity; its IMGUI approximation is not a TMP
rendering test.

Manual checklist (all Unity checks pending):

- [ ] Import in 6000.3.26f1, run EditMode/PlayMode tests and validate content.
- [ ] Preview all 32 entries and all four lifetimes, with nested spans.
- [ ] In Play Mode, watch fear/wave after reveal completes; dismiss/replace and
      verify baseline restoration. Resize the window while effects run.
- [ ] Toggle reduced motion, intensity zero, effects off and flashes off live.
- [ ] Verify alpha flash cadence with full motion; authored speed cannot bypass safety.
- [ ] Test the optional style asset overrides and both day/night palettes.
- [ ] Profile warmed TMP updates and mesh rebuilds separately; record actual
      allocations/frame cost. No Unity profiling or build is claimed here.
