# Milestones

Implement and review one milestone at a time. Milestone 00 contains no gameplay.
Each PR, or specifically confirmed direct publication to `main`, updates README
and ARCHITECTURE, lists actual checks and unavailable checks, and contains only
its milestone's scope. Do not merge automatically.

| Milestone | Branch | Scope | Acceptance gate |
| --- | --- | --- | --- |
| 00 | `milestone/00-project-foundation` | Configuration, folders, metadata, assembly boundaries, architecture, design and authoring docs | Static checks pass; editor import explicitly recorded as pending until run |
| 01 | `milestone/01-core-loop` | Seeded seven-character cast, five initial roles, full day/trial/night loop, basic intent-based NPCs and placeholder dialogue, plain text UI | Complete a Town run and an Assassin run; tests for phases, votes, target legality, night interactions, outcomes, determinism, and information isolation |
| 02 | `milestone/02-social-reasoning` | Personality tuning, attributed evidence, uncertain claims, contradictions, exposure estimates, team planning, authoring importer and anti-repetition | Reproducible scenarios with competing claims; no secret-state access; data-only new lines work |
| 03 | `milestone/03-role-interactions` | Remaining starter roles, constrained role-list generation, advanced priority policies, capability-aware outcomes and stalemates | Invalid setup reports and role interaction matrix; viable player tools for each role |
| 04 | `milestone/04-save-resume` | Versioned snapshots, complete stream/state persistence, load validation, settings | Identical continuation after save/resume, including pending input, beliefs, resources and dialogue selection |
| 05 | `milestone/05-presentation-effects` | Cue profiles, semantic markup, skip/accessibility, typewriter, color, shake, vignette/border, limited flash, playground | Parser and sequence/blend/cancel tests; editor preview; no allocations during steady effect updates |
| 06 | `milestone/06-content-balance` | Owner-authored content coverage, difficulty tuning, simulation reports, refinement of antagonist/neutral agency | Reproducible simulation data plus human playtesting; no hidden-state advantage |

## Milestone 00 verification

Milestone 00 was published to `main`. Milestone 01 now has a feature branch with
the loop, assets, scene, editor content validation, and behavioral tests. Shared
C# code compiled and 33 tests passed in its first GitHub Actions run, including
100 seeded runs across Town and antagonist player roles. Subsequent test/schema
updates are recorded in `Docs/VALIDATION.md` and the PR checks.

The next required external gate is Unity import and real scene play, using
`Docs/UNITY_VERIFICATION.md`. Complete this before moving to milestone 02 or
claiming a verified Unity build. No Unity editor/license is available in the
creation environment; headless tests cannot check TMP resources or uGUI input.

- Run `python3 Tools/validate_scaffold.py`.
- Review `.asmdef` dependencies and metadata pairs.
- Check `git diff --check` and ignored Unity/IDE/credential files.
- Open with Unity 6000.3.26f1, resolve packages, commit generated lock/settings,
  and record editor import results when an editor is available.
- Expect no playable scene and no discovered gameplay tests yet.

## Milestone 01 vertical slice

The minimum playable experience includes the player in the cast, suspicion and
claims, public nominations and judgement, last words, death/reveal, night target
selection and scoped results, repeatable days/nights, and a terminal outcome.
At least one fixed scenario must exercise a blocked attack and another a
protected target. The player can be accused, tried, killed, and spectate after
death. A simple evidence-based NPC strategy suffices initially; it must use
restricted knowledge from the start.

The branch contains one game scene with composition root, a uGUI transcript/choice panel,
Input System UI wiring, authored role assets and an explicit content
catalog. Its initial role-list constraints are code, with general setup assets
deferred to milestone 03. Import TMP essential resources through Unity;
preserve its generated metadata and font licensing. Route every displayed line
through the text presenter. No typewriter, animated text, flash, shader, or
post-processing work belongs in this slice.

## Effects milestone prerequisites

Before code, confirm the selected render setup and propose cue mapping,
effect interfaces, conflict rules, cancellation, and display acknowledgement.
The slice includes typewriter, semantic color, text shake, vignette/border,
and a limited flash. Expand only after that slice works.

Provide a markup cheat sheet and recommended semantic styles per speech intent.
Map dawn/dusk, accusation/nomination, trial/defense, guilty/innocent verdicts,
execution/reveal, night reports, targeted/blocked messages, investigations,
ties, victory/defeat, player kills, near exposure, and narrow escape to cues.
Additional candidate cues are teammate loss, a contradicted claim, depleted
ability resources, and a final-three confrontation. These remain semantic
events; profiles own visual interpretation.

The application supplies player-visible tension only. Accessibility defaults
limit flashes and provide disable, reduced-motion, intensity, and skip controls;
text/symbols must retain every distinction otherwise shown by color. Add unit
tests for markup validation, sequence/parallel timing, blending, cancellation,
and exactly-once acknowledgement. Add an editor-only playground with intensity
controls. Full effect expansion is subsequent work within the same reviewed scope.
