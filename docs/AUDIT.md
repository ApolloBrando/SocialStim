# Mission audit — phase 0

Base: fc8f5cde97f1d2acba26e2e54a218179604ad153. All 424 tracked files were
fetched in full: 144 C# files, 7 asmdefs, 204 metadata files, authored assets,
the scene, tests, docs and mission. See [inventory](AUDIT_INVENTORY.md).
No AGENTS.md exists. Existing metadata GUIDs are valid, unique and preserved.

Unity 6000.3.26f1; Input System 1.17.0; Test Framework 1.6.0; uGUI/TMP 2.0.0.
There is no usable local execution environment or Unity editor. Shared C#
checks run through GitHub Actions. Package lock and editor-generated Player
settings need a real import; no Unity-generated YAML is synthesized here.

## Architecture

| Assembly | Project dependencies | Responsibilities |
| --- | --- | --- |
| Rules | none; no engine | RunFactory/RoleList, RunSession/PhaseMachine, night stages, beliefs/strategies, dialogue selection and outcomes |
| Application | Rules; no engine | GameController/options, display playback, semantic parsing and visible cues |
| Data | Rules | Unity definitions/catalog plus engine-free JSON importer |
| Presentation | Rules, Application, Data | Bootstrap/presenter/view, cards, TMP renderer, overlay and preferences |
| Editor | Rules, Application, Data | Content validation and effects playground |
| Tests.EditMode | Rules, Application, Data | Rules, information isolation, content and portable presentation |
| Tests.PlayMode | all runtime assemblies | Presenter gating, TMP geometry and palettes |

Bootstrap converts referenced assets into immutable records. Presenter routes
revision-scoped options through Controller. Session validates commands, chooses
seeded NPC actions and updates observer evidence before emitting scoped records.
The view reveals observations and gates input until display acknowledgement.
NightResolver orders block, forge, visits, protection, attack, watch, investigate.
Reports reach only authorized living recipients.

Asmdefs respect layer direction; Rules/Application have no Unity references.
Session and TextGameView own too many responsibilities; Controller expands
every target/claim/result into a large flat list. Extract focused services while
retaining typed ports, composition and constructor injection.

## Findings

Line numbers refer to the audited base.

| ID | File:line | Root cause | Disposition |
| --- | --- | --- | --- |
| P0-01 | `Assets/_Project/Presentation/Text/TextGameView.cs:250` | A preference change reveals and acknowledges all queued lines. | Fixed: reconfigure current playback without flushing its queue. |
| P0-02 | `Assets/_Project/Presentation/Effects/TextEffectRenderer.cs:64` | Motion uses a finite cue envelope and a stopped playback clock; completion clears shake. | Fixed initial continuous fear motion using live display time; generalized effect lifetimes in Phase 6. |
| P0-03 | `Assets/_Project/Presentation/Text/TextGameView.cs:246` | Repeated errors append previous errors/status until they overflow. | Fixed: replace the error. |
| P0-04 | `Assets/_Project/Presentation/Text/UiPreferences.cs:20` | One corrupt numeric preference resets every valid preference; unknown versions discard everything. | Phase 3: versioned JSON, per-field clamping/migration, reset and file adapter. |
| P0-05 | `Assets/_Project/Presentation/Text/TextGameView.cs:167` | Settings are index-coupled footer controls with no reset or edit semantics; expanded footer crowds narrow screens. | Phase 3: grouped settings screen from one drawer. |
| P0-06 | `Assets/_Project/Application/PresentationCues/EffectsSettings.cs:9` | Default is 28 graphemes/s; speech tempo is 0.78–1.22 with 0.65–1.64s holds. | Phase 3: 84/s, Instant, first press reveal/second advance, length-based optional auto-advance. |
| P0-07 | `Assets/_Project/Rules/Phases/RunSession.cs:164` | Any participant can claim any role or fabricate an alignment; no per-role permissions. | Phase 1: shared truthful/bluff permissions for player and NPC. |
| P0-08 | `Assets/_Project/Rules/Phases/RunSession.cs:92` | Discussion is one player action followed by every NPC; no exchange director or priority response. | Phase 2: rounds, bounded windows, response interrupts and reaction sequences. |
| P0-09 | `Assets/_Project/Rules/Commands/VoteResolver.cs:11` | Only strict-majority nomination and trial judgement exist. | Phase 2: plurality ballots, one tie revote, abstention rules, per-ballot reveal; optional legacy trials. |
| P0-10 | `Assets/_Project/Rules/Victory/CapabilityWinCondition.cs:25` | Legacy parity waits for combat/protection; no Infinitum, and lone-killer victory permits benign survivors. | Phase 1: mission outcome precedence, steal and strict Anarchy last-survivor tests. |
| P0-11 | `Assets/_Project/Rules/Night/ProtectionStage.cs:19` | Protection raises defense rather than canceling one attack; no actual attack notice or self-heal resource. | Phase 1: patient heal tokens, attack receipt and configurable self-heal. |
| P0-12 | `Assets/_Project/Rules/Night/ForgeryStage.cs:14` | Forgery forces an alignment instead of inverting CLEAR/CORRUPT; no ANOMALY. | Phase 1: typed readouts, invert-only forge and night immunity. |
| P0-13 | `Assets/_Project/Rules/Reasoning/BeliefDistribution.cs:54` | Independent heuristic faction weights do not represent jointly consistent role worlds. | Phase 4: bounded hypothesis worlds and visibility-aware marginals. |
| P0-14 | `Assets/_Project/Rules/Reasoning/BeliefState.cs:71` | Flat belief bonuses omit affinity, stats and declared reason strength. | Phase 4: persuasion service and retrospective evidence updates. |
| P0-15 | `Assets/_Project/Rules/Reasoning/SocialNpcStrategy.cs:131` | Independent utility scores lack reason codes, vote prediction and new faction objectives. | Phase 4: expected utility, faction plans, bounded errors and simulation. |
| P0-16 | `Assets/_Project/Data/Definitions/GameCatalog.cs:19` | 20 shuffled names have no curated voices, trait ranges or signatures. | Phase 5: at least 16 authored identities with seeded variation. |
| P0-17 | `Assets/_Project/Rules/Dialogue/DialogueSelector.cs:18` | Only five variables and per-speaker three-line memory; no reasons, tones, archetypes, truth/stat gates or global cooldown. | Phase 5: extended schema and measured coverage without relaxing factual eligibility. |
| P0-18 | `Assets/_Project/Presentation/Text/TextGameView.cs:282` | Each line rebuilds the whole growing transcript and semantic document, recapturing TMP geometry. | Phase 2/6: focused utterance, separate bounded log and cached span effects. |
| P0-19 | `Assets/_Project/Presentation/Text/TextGameView.cs:278` | Claim styling discards authored nested spans. | Phase 6: external badge and preserved span metadata. |
| P0-20 | `Assets/_Project/Rules/Cast/RunFactory.cs:27` | Fallback setup is coded; no complete player-facing setup/min-max/presets/visibility editor. | Phase 1/3: validated setup model and live New Run editor. |
| P0-21 | `Assets/_Project/Presentation/Text/GameBootstrap.cs:70` | One monolithic view lacks distinct main, vote, night, morning and after-action screens. | Phase 2/3: screen coordinator and thin scene adapter. |
| P0-22 | `Assets/_Project/Rules/Phases/RunSession.cs:310` | Session owns phase flow, claims, beliefs, team planning, night resolution, results and prose. | Extract council, claims, night coordination, outcome and report services by phase. |
| P0-23 | `.github/workflows/validate.yml:5` | Push CI excludes phase/**. | Fixed: include phase/**; PR CI stays enabled. |
| P0-24 | `Tools/HeadlessTests/SocialStim.HeadlessTests.csproj:18` | Headless compilation excludes Unity adapters, Editor and actual layout/input. | Portable regressions plus explicit manual Unity checklist per phase. |

## Settings, UI and text

The footer lacks grouping, defaults, migration, versioned JSON, Apply/Cancel
and game customization. Numeric control indices couple labels to actions;
expanded buttons crowd narrow screens. One bad number resets all preferences.
Phase 0 fixes the accidental batch flush. Phase 3 replaces storage/edit semantics.
Audio is absent: do not add an empty Audio category.

[AUDIT_STRINGS.md](AUDIT_STRINGS.md) inventories display literals/dispositions.
Remove repetitive phase narration, animation-only “Listening…” status, repeated
target mechanics, page-count filler, nested menu instructions and repeated
setup controls prose. Preserve flavor, ballots/tallies, deaths, claims/results,
concise errors and accessible names. Seed, own objective, history, public ledger,
marks and settings move behind one drawer. Ledger lists assertions, not solutions.

## Performance and shake

TMP Paint reuses arrays and allocates no obvious per-frame collections. Capture
grows buffers, but each appended line rebuilds the complete transcript and mesh.
StyleAt scans all spans per character at capture. Growing history is quadratic
over a run. Use one focused utterance and a separate bounded/virtualized log.
Belief/night snapshots allocate per turn, not per frame; sampling needs a budget.

Shake root cause: a finite cue envelope supplies all amplitude; Finish clears
the frame and Tick stops elapsed time. Text loses amplitude and clock after
completion. Phase 0 adds continuous fear amplitude on live unscaled display time,
separate from the cue pulse. Reduced motion/effects-off stop it. Phase 6 adds
generalized span lifetimes, effect stacks and bounded mesh work.

## Dead, duplicate and complex code

BasicNpcStrategy is injected by tests; StarterWinCondition serves compatibility
catalogs; EffectSequence builds default profiles. None is proven dead. Retain
until an explicit migration or optional Trial Mode replacement. Saving contains
only an empty tracking file: no save implementation exists. No uncertain
deletions in phase 0; every actual deletion will appear in its phase PR.

Repeated wording/palette/phase decisions and nested menu builders are migration
targets. Existing Docs remains historical and will point to current docs.

## Missing tests

Player/NPC claim permissions and unique counterclaims; readouts; one heal versus
multiple attacks; Infinitum immunity/steals; Anarchy last survivor; promotion and
backlash timing; stable vote/night outcome boundaries; plurality/revote/abstain;
windows/responses; setup limits/visibility; stats/affinity persuasion; worlds
consistency/budget; settings clamp/migration/reset/file round trip; unresolved
variables/global cooldown/coverage/near duplicates; continuous effects and span
ownership; reveal/advance separation; simulation outcomes and stalls.

## Decisions and phases

0. Audit and immediate display/settings/error/CI fixes.
1. Core roles, claim permissions, readouts, resources, outcomes and setup model.
2. Council rounds/windows/reactions, plurality voting and focused interaction
   screens. Keep working trials only as an explicit option.
3. Setup, grouped/versioned settings, 84/s reveal (3 × current 28), Instant,
   optional length-based auto-advance.
4. Stats, affinity, reason-coded inference/utility and simulation.
5. Curated voices and >=3500 static lines in authored batches with measured coverage.
6. Persistent effect catalog, markup/styles and playground.
7. Optional Free Time/extra roles after core integration.

Each phase has a feature branch/PR; dependent branches stack with an explicit
base. Never write main or merge a PR. Main Menu Continue means only the active
in-memory run: no gameplay save files or cross-run progression.

Parity requires living Evil: 0 >= 0 never triggers a steal. Infinitum cannot
share Town victory. Anarchy + Infinitum alone draws immediately: immunity and
reciprocal two-person vote ties otherwise stall forever. Aperture changes
Decryptor readouts, not Physician notices. Heal cancels one attack, not every
attack. Default Archon promotion occurs before terminal checks.

Publisher references consulted: https://playism.com/en/game/gnosia/ and
https://playism.com/gnosia-special/ establish discussion/voting/night attacks,
relationships and character abilities. Exact rounds/windows/UI come from the
mission. No reference dialogue/assets are copied.

## Manual phase-0 checklist

- Import in the pinned editor; resolve TMP/Input System; run both Unity suites
  and Validate Starter Content.
- During a long line, change speed/intensity/effects/reduced motion. Reveal must
  not restart and pending lines stay pending.
- Enable motion and wait after fear text finishes typing. Current-span motion
  continues; reduce motion/disable effects and it stops.
- Repeated invalid setup attempts show only the newest error.
- Test restart/cancel, skip, palettes and no stuck hidden choices.
- Inspect Full HD/narrow aspects. Old-footer crowding remains a later UI task.

Shared-check receipts are recorded in the PR. No Unity result is claimed.
