# Presentation effects: milestone 05

Presentation retains Screen Space - Overlay uGUI/TMP and the milestone 05
effect contracts, extended by milestone 03's playtest changes below. No camera,
URP, shader, post-processing, runtime generation service, or new package is
needed. Ongoing-run save/resume has been dropped. Only presentation preferences persist.

## Display and input contract

`GamePresenter` supplies scoped requests to `IGameView`. The view queues only new
transcript observations. Each started line creates one `DisplayPlayback` transaction:
parsed text, profile, settings, sequence ID, and completion callback. `Tick` samples
portable channels and reveals complete text elements using cached boundaries.

The callback fires exactly once as Completed, Skipped, or Cancelled. It is cleared
before invocation, including under reentrant skip/cancel. Rules already completed
the command; effects never call rules, consume RNG, or determine outcomes.
`IGameView.IsDisplaying` blocks the next player command in the presenter as well
as disabling choice buttons. Skip reveals the whole queued batch, clears screen pulses and retains explicitly
continuous motion on the final current line until replacement/cancellation. New run cancels the active line and discards unstarted lines;
unstarted lines never acquired a display acknowledgement. Disable resets display.

The renderer builds strings, parses markup and caches mesh buffers at line/layout
boundaries. Steady `Tick`, `Apply`, and channel evaluation use bounded loops and
reused data. TMP layout/font atlas rebuilds and first-capacity growth are separate
allocation boundaries. A headless allocation test covers the portable clock;
Unity profiler validation after warm-up remains required.

## Cozy palettes and controls

Day uses dark text on warm paper-like panels; night uses light text on slate
panels. Visible delivered speech and trial progress warm the day background
toward soft red. Hidden selections do not drive the palette. Rounded surfaces,
subtle frames and shadows remain when effects are off. Focus uses both a sage
tint and the textual > marker.

The footer shows Skip and Display options. The separate options panel contains
effects, motion, flashes, instant text, intensity, speed and Reset display defaults.
Defaults are reduced motion, flashes off, 50% intensity and 84 text elements/second.
Presentation preferences persist with PlayerPrefs; no run state is written.
Changing a display policy finishes the pending batch first.

Reduced motion suppresses shake and flash; gentle alpha cues remain. Effects off
reveals text immediately, disables semantic accents, and clears every channel.
Instant text preserves independent cue timing. Color never supplies missing
game facts: accusations, votes, deaths and outcomes retain their actual words.
There is no automatic camera shake or constant ambient noise.

Flash is a single smooth low-opacity tint rather than a strobe. Profiles reject
multiple flash tracks and flash durations under one second. Output is capped at
8% opacity at full intensity, with a two-second start cooldown across lines.
Reduced motion suppresses it even if the flash preference is enabled. Phase
palette inversion is separate from the optional flash channel.

## Semantic markup cheat sheet

| Syntax | Presentation | Suggested use |
| --- | --- | --- |
| `<emphasis>text</emphasis>` | Stronger neutral ink | A decisive phrase, last words |
| `<fear>text</fear>` | Muted violet accent | Fear or uncertainty |
| `<calm>text</calm>` | Muted green accent | Defense or reassurance |
| `<evidence>text</evidence>` | Muted blue accent | An asserted finding; never proof of truth |
| `<danger>text</danger>` | Muted red accent | An accusation or warning |
| `<<` | Literal `<` | Comparisons or literal tag examples |

Tags are case-sensitive, properly nested, and limited to eight levels. The inner
style wins. One source line allows 32768 UTF-16 units. Unknown tags, attributes,
unmatched tags, raw TMP tags and sprite/material injection are rejected. Rules
still select opaque text; only presentation parses it. Runtime displays malformed
text literally with TMP rich text disabled. Tools > SocialStim > Validate Starter
Content rejects invalid authored markup. The starter lines demonstrate the supported syntax. The Mission part two assigns
future authored dialogue expansion to implementation; the current pool has 70 lines.

| Speech intent | Recommended emphasis |
| --- | --- |
| Accuse / CallForTrial / Deflect | A short danger phrase |
| Defend / Vouch / Agree | Calm, or neutral text |
| ClaimRole / ReportInvestigation | Evidence on the assertion, regardless of truth |
| Disagree / VoteJustification | Emphasis on the reason |
| ExpressFear / ReactToDeath | Fear used sparingly |
| ReactToRoleReveal / LastWords | Evidence or emphasis |
| StaySilent | Neutral |

## Cue vocabulary

`VisibleCueResolver` reads only player snapshots, public assertions and scoped
observations. Added rule metadata is factual: delivered intent, asserted role,
public nominee, death subject and trial defendant. It never contains a pending
hostile target, an unrevealed role, or an NPC's private belief model. Observation
enum values were appended to preserve the existing numeric meanings.

| Visible event | Cue / default treatment |
| --- | --- |
| Dawn / dusk | Dawn / Dusk; gentle border, phase palette inversion |
| Accusation / nomination | Accusation / Nomination; tense / gentle border |
| Trial / defense | Trial / Defense; tense / gentle emphasis |
| Guilty / innocent | Guilty / Innocent; decisive / gentle cue |
| Execution / public death / reveal | Execution / Death / Reveal |
| Private blocked / investigation report | Blocked / Investigation |
| Survived an attack | NarrowEscape; tense cue only after the legitimate report |
| Failed strict majority | Tie; same initial no-majority meaning as the rules cue |
| Victory / defeat / draw | Victory / Defeat / Draw |
| Player death | PlayerKilled takes precedence over generic death |
| Player put on trial | NearExposure; public pressure, not secret exposure estimates |
| Known teammate dies | TeammateLost takes precedence over generic death |
| Speaker switches a claimed role | ContradictedClaim; assertion history only |
| Last protection charge used | ResourcesDepleted; own legitimate result and resource count |
| Public death leaves three participants | FinalThree, unless player/teammate death takes precedence |

Ordinary PhaseChanged observations and generic speech have no screen pulse.
There is no continuous tension score, hidden-target warning, or producer for
future role mechanics. Profiles may map these semantic meanings differently.

## Profile authoring and conflicts

`StarterEffects.asset` is assigned to Game.unity. It contains a sample Dusk override;
unlisted cues use `DefaultCueProfiles`. Create another profile set with Assets >
Create > SocialStim > Cue Profiles and assign it to GameBootstrap. Definitions
are copied and validated at startup, not consulted per frame. Duplicate cues,
invalid tracks, more than 64 tracks, and unsupported flash patterns are rejected.

Each track specifies channel, delay, duration, amount, priority, blend and envelope.
Channels are independent TextShake, Border, Vignette and Flash. Sine, Triangle
and Smooth envelopes begin/end at zero. Authoring delays allow overlap or sequence;
the portable builder offers Parallel followed by Then for explicit composition.

Replacement tracks choose the highest priority; equal priority chooses the later
authored track. All additive tracks then sum over the winning replacement, with
the final channel clamped to [0,1]. Different observations play sequentially.
Cancel clears every channel and restores baseline vertices. Skip clears screen
pulses but preserves explicitly continuous TextShake on the current line.
Semantic text color is a cached style channel, not a source of facts. All screen
geometry ignores raycasts and uses the existing UI material.

## Editor preview and remaining gates

Tools > SocialStim > Effects Playground previews default profiles and the portable
clock, day/night palettes, intensity, reduced motion, flash opt-in, text reveal,
skip and cancel. It parses the same markup and reports acknowledgements. It does
not modify a scene. This IMGUI preview approximates screen geometry; actual TMP
glyph styling, profile asset overrides and layout require Play Mode.

Follow [EFFECTS_VERIFICATION.md](EFFECTS_VERIFICATION.md). Headless compilation and
tests establish portable behavior, not Unity import, adapter compilation, actual
screen appearance, font resources, navigation, builds, or runtime allocation results.

## Latest playtest palette and pacing

The owner's latest correction supersedes the original inverse palette: daytime
has a light background with dark text, and nighttime a dark background with
light text. Daytime warmth progresses from pale yellow toward soft red as
public spoken lines and trial stages accumulate; it uses no real gameplay
clock or secret tension. Fixed frames remain visible with effects disabled.

Default reveal speed is 84 text elements/second, with a stable per-speaker tempo and
0.20–0.54 second per-line reading holds. These derive from public observation
IDs and do not use rules RNG. Skip bypasses the holds. Instant text bypasses
typing/reading holds while enabled cue effects retain their own duration.
Role claims receive a ROLE CLAIM label, Evidence styling and an unverified
claim board. Participant cards are ordinary uGUI objects. Choices and public
status/cast updates wait for display completion.

The cozy UI uses rounded native surfaces, soft frames/shadows, warm daylight,
a slate night palette and a sage focus tint. Text speed and accessibility
preferences persist independently of runs; there is no game save/load UI.

## Continuous motion repair (mission phase 0)

EffectStep supports explicit continuous text motion. Other channels reject this
mode, so screen/flash pulses cannot become unbounded. Text-motion intensity still
obeys enabled/reduced-motion/intensity settings. EffectSequence preserves the flag;
EffectTrackDefinition exposes it as a private serialized inspector option.
The default tense cue retains shake. Other authored pulses remain finite unless
explicitly changed. The clock continues after acknowledgement without blocking
input or sending another callback; replacement/Cancel clears the retained state.
This repairs cue motion lifetime. Independent span stacks and the extended effect
catalog remain planned work, with their validation listed in AUDIT.md.
