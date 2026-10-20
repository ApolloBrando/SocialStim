# Mission progress against the current repository

Assessment: 2026-10-20, current-main baseline `7f6be12` plus the repairs in PR #24.
The owner's later CONTRIBUTING change is preserved. This assesses actual code and
assets in the working tree, not feature-branch PR descriptions. README files remain
owner-managed. Free Time is cancelled and is excluded from remaining work.

## Progress by mission area

| Mission requirement | Present now | Status / remaining work |
| --- | --- | --- |
| Audit and boundaries | Seven asmdefs, portable rules/application, typed IDs, seeded RNG, strict importer, metadata and dependency checks; post-merge findings and regressions | Substantial foundation. Native Unity import/build/input/profiling verification remains open |
| Day discussion and vote loop | Default five rounds; configurable 3–7, 1–2 player windows; opening yield, bounded NPC answers and priority player responses; plurality ballots, tally, one restricted revote, abstention, last words and reveal toggles | Core loop implemented in RunSession and covered by tests. Richer question/answer, agreement chains and persuasion remain partial |
| Day/night-only scope | Discussion/vote/elimination → night actions → morning → discussion; no Free Time runtime phase in this baseline | Implemented scope. Old mission requirements corrected again in PR #24 |
| Gnosia-style interaction/UI | Native text view, paginated category/family choices, public roster, claims, focus indication and phase palettes | Partial. Missing CHAT/ACCUSE/DEFEND → roster target → up to three exact authored phrases; current-line screen, unified drawer, marks, Quick mode, tutorial and after-action review |
| Claim/bluff legality | Claims are attributed and contradictions affect credibility | Incomplete. No CanClaimTruthfully/CanBluff role contract or shared constrained menu/AI legality; ordinary Town can still bluff |
| Mission nine-role set | Eleven legacy role assets exist: Inspector, Physician, Interrupter, Assassin, Survivor, Citizen, Vigilante, Forger, Coordinator, Provocateur and Lone Killer | Incomplete. These are not the required Solarian/Decryptor/Nullifier/Archon/Singer/Aperture/Anarchy/Infinitum mechanics. Shared names/rough equivalents do not establish completion |
| Mission outcomes/night rules | Ordered block → forge → visit → protection → attack → watcher → investigation pipeline; charges, simultaneous attacks, apparent-alignment results and legacy capability outcomes | Partial infrastructure. Missing three-way readouts, Infinitum immunity/parity steal, strict Anarchy survivor rule, one-attack healing/patient notices/self-heal, backlash and promotion |
| Setup/customization | Role slots, scalable faction shares, deterministic random/chosen player role; authored participant count and loop options | Partial. No full setup UI, presets/custom min–max editor, faction selection, visibility policy, difficulty/stats/point-buy, role-specific switches or live setup controls |
| Settings overhaul | Display effects, reduced motion, flash, instant text, intensity and 28/84/168 speed controls; reset; PlayerPrefs persistence | Partial. Live-change queue bug repaired. Versioned JSON migration/clamping, game/text/accessibility groups, UI scale/text size, auto-advance and debounced persistence remain |
| NPC knowledge and AI | Observer-local own role/team/private reports; public evidence, normalized faction/role estimates, credibility, exposure, trait utilities and team-kill coordination; hidden-assignment regressions | Partial. No sampled consistent worlds, independent affinity/stats, difficulty tiers, faction-specific mission plans, vote utility/tie prediction, reason/truthfulness dialogue contract or debug overlay |
| Editable content | 70 unique spoken JSON lines across 16 intents; stable IDs, weights, traits/role/context conditions and optional presentation metadata; anti-repetition | Small foundation. UI/narrator/night reports/menus remain hardcoded. No full editable copy catalogue/editor in this baseline |
| Dialogue volume and cast | 20 distinct names; 4–5 generic variants per current intent | Far short. Need 16 voiced cast profiles × 8 signatures, ten archetypes, 15 core/6 minor variants per archetype, 500 authored player options, event variants and about 3,500 unique lines. Names are not completed character profiles; parameterized buttons are not authored dialogue lines |
| Dialogue tooling | Strict schema/known-token/condition validation, fallback coverage and markup tests; malformed braces now rejected | Partial. Missing reason/truthfulness/stat metadata, near-duplicate and target-coverage reports, run/global cooldowns and complete authoring catalogue |
| Effects | Six style values (default + five semantic tags), one glyph movement family, screen border/vignette/flash cues, safe defaults and continuous shake | Partial. Default reveal speed is 84 elements/sec, up from 28. Need 25+ glyph effects, full lifespan modes/parameterized styles, per-speaker/tension defaults and actual TMP playground. Static settled mesh uploads reduced, without a measured performance claim |
| Unity workflow | ScriptableObject data, uGUI/TMP, native EventSystem/Input System UI module, editor menu and IMGUI playground; PR #24 adds safe EventSystem reuse and shared all-content/build validation | Partial. Missing current input-action ownership/remapping and pointer flow, import callbacks, authoring editor/inspectors, catalogue export and profiler instrumentation from earlier feature work |
| Simulation/balance | Existing seeded integration runs plus 352 full-catalogue smoke games in PR #24, with repeated-seed comparisons | Regression coverage, not the requested thousands-game AI-only benchmark, faction win-rate/role-impact report or tuned difficulty baseline |
| Extra polish/roles | Legacy watcher/forger/provocateur/lone-killer roles exist; seed is displayed | Core mission must stabilize first. No extra-role completion or balancing claimed; no Free Time work remains |

## Exact current dialogue counts

| Intent | Unique spoken lines |
| --- | ---: |
| Accuse | 5 |
| Defend | 5 |
| ClaimRole | 5 |
| ReportInvestigation | 5 |
| Vouch | 5 |
| ReactToDeath | 5 |
| CallForTrial | 4 |
| Agree | 4 |
| Disagree | 4 |
| Deflect | 4 |
| StaySilent | 4 |
| ExpressFear | 4 |
| ReactToRoleReveal | 4 |
| LastWords | 4 |
| VoteJustification | 4 |
| ReportAction | 4 |
| **Total** | **70** |

All 70 text values are distinct. This is 70 shared spoken variants, not 70 fully
voiced characters, 500 player-option lines, or the 3,500-line mission target.
No new dialogue volume was authored during this bug-cleanup pass.

## What this repair adds

Confirmed fixes cover live-settings conversation skipping, settled and stale-frame
motion policy, unnecessary static mesh uploads, EventSystem duplication, basic Town
killer targeting, custom-catalogue bluffs, malformed placeholders and peaceful-morning
casualty effects. Workflow changes cover case-safe audit retention, missing serialized
references, conflict markers, ignored generated output, shared editor/build content
validation and broader CI guards. Named settings controls replace fragile indices.
See `POST_MERGE_AUDIT.md` for root causes, evidence and native verification steps.

## Remaining mission board

1. Integrate the actual mission role model, outcomes and setup configuration into one
   supported game path, with shared claim/report permissions and information boundaries.
2. Complete settings persistence/migration and runtime setup controls, including stats,
   difficulty and role-list visibility. Keep every run self-contained.
3. Complete the current-line/roster interaction UI and exact authored player choices;
   externalize all remaining product wording into editable data.
4. Implement the sampled-world inference/social layer and explicit decision reasons;
   connect those reasons, truthfulness and stats to dialogue eligibility.
5. Author the cast and large dialogue corpus, with reliable editing and coverage tools.
6. Complete expanded glyph effects/lifetimes and Unity input/authoring/profiling workflow.
7. Run native Unity compilation/tests/builds/device checks and the full AI-only balance
   simulations. Portable success cannot close those gates.

No defensible single completion percentage is assigned: surviving legacy foundations,
missing integration, content volume and native verification are different kinds of work.
