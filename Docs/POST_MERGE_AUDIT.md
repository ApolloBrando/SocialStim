# Current-main audit and repair

2026-10-20. Starting code snapshot: main `7f6be123d6a7090a58b004bc8af29c3fd095932b`.
The owner's subsequent `c04b3a2b14cd375d9aa4f4af6e6aed24fc43d135` CONTRIBUTING
change is preserved. Work is on `fix/post-merge-audit`, PR #24. README files are untouched.

## Main is not the union of the earlier feature stacks

The surviving game is the RunSession/TextGameView implementation. Main's first-parent
history includes #8, #13 and #14, but does not contain the reviewed council tip
`60885e5`, council cancellation tip `d519939`, or UI/tooling/scope tip `31f0c98`.
`git merge-base --is-ancestor <tip> origin/main` returns false for each.
Merges into a child feature branch do not integrate that child's changes into main.
This review does not assume missing systems were deliberately discarded or claim
they were removed by a particular conflict resolution.

Absent from this baseline: CouncilSession, mission nine-role rules/readouts,
sampled-world inference, full settings/custom setup services, the 32-effect renderer,
current-line/roster UI, the external runtime-copy catalogue and its authoring editor.
These are unfinished integration work. They are not counted as implemented below.
No old stack was merged wholesale during this repair.

## Review scope and architecture

The starting tree contained 443 source/configuration/documentation/metadata files,
including 151 C# files and seven assembly definitions. Inspection covered the rules,
application flow, Unity composition/UI/effects, content importer, tests, authored
roles/setup/dialogue/cue assets, scene/build references, package pins, and validation
workflow. Source review and automated repository checks do not verify a Unity import.

| Layer | Surviving responsibility | Boundary |
| --- | --- | --- |
| Rules | RunFactory, RunSession, phase/discussion scheduling, ballots, roles, ordered night stages, observer beliefs and strategies | Portable C#; seeded IRandomSource |
| Application | GameController revision-scoped choices, display playback, semantic markup and cue mapping | Portable C#; no Unity references |
| Data | ScriptableObject roles/setup/catalogue/reasoning plus portable strict dialogue JSON importer | Converts assets to detached immutable data |
| Presentation | GameBootstrap → GamePresenter → TextGameView; native uGUI/TMP, effects, PlayerPrefs | Receives scoped snapshots, owns input/rendering |
| Editor | Content validation, scene opening and effects playground | One-way references to runtime definitions; no reverse rule dependency |
| Tests | Shared NUnit behavior; separate native TMP/uGUI tests | Portable and Unity verification remain distinct |

Runtime path: bootstrap loads catalogue/setup → factory creates isolated seeded run →
controller exposes permitted choices → presenter submits one revision-scoped ID →
session resolves rules and appends scoped observations → view reveals observations
before making the next choices available. The view never resolves votes or night actions.

## Confirmed issues repaired

| Location | Root cause / observed implication | Repair |
| --- | --- | --- |
| `TextGameView.ChangeSettings` | Called SkipAll and Cancel after playback.Reconfigure, consuming queued conversation whenever speed or accessibility changed | Apply the policy to current visuals without acknowledging the queue; native button regression added |
| `DisplayPlayback.Reconfigure` | Returned immediately for settled text, leaving an active continuous-motion frame under the old policy | Resample settled motion immediately; test disable/reduced-motion changes without a second acknowledgement |
| `TextEffectRenderer.Paint` | Trusted the supplied shake frame even when its own policy disabled effects/reduced motion | Gate actual geometry movement on renderer policy, including stale frames |
| `TextEffectRenderer.Apply/Initialize` | Reuploaded static settled text every frame; repeated initialization could leave old TMP callback subscriptions | Dirty-state/reveal/motion guard and explicit callback ownership; no measured profiling claim |
| `TextGameView` settings controls | Hardcoded list indices tied labels/focus to button creation order | Named button references; remove redundant semantic-color setter |
| `GameBootstrap.Start` | Always created another EventSystem | Reuse a compatible enabled native system, create one only when absent, report duplicates/incompatible modules |
| `BasicNpcStrategy.NightTarget` | Town attack shared protection's ascending suspicion sort, shooting the least suspicious unconfirmed person | Town attacks strongest eligible suspect only above threshold; other attack/protect policy stays separate |
| `BasicNpcStrategy.Speak` | Bluff hardcoded role ID `inspector` even for custom catalogues | Select an available authored Town role, preferring investigation |
| `DialogueSelector` | Token regex recognized valid placeholders but missed unmatched/nested braces; null rows failed obscurely | Reject malformed braces and null inputs before selection; importer regressions |
| `VisibleCueResolver` | A no-death morning report used the death cue family and triggered casualty effects | No target means no casualty effect; regression uses a real peaceful night |
| `ContentValidation` | Only the starter catalogue was checked; forcing every catalogue role into each custom setup rejected valid excluded-role lists | Discover every project catalogue and role/setup/reasoning/cue asset; validate actual setup constraints; report asset path and dialogue ID |
| Native build workflow | Manual validation could be skipped before a player build | Shared content checks run through IPreprocessBuildWithReport; fail the build on invalid content |
| Repository checks | Did not detect the case-colliding audit files, conflict markers or dangling serialized GUIDs | Add all three checks; preserve both historical audits under distinct filenames |
| Repository checks after builds | Scanned ignored generated files/directories as if they were source | Use Git's tracked/untracked nonignored source inventory; test actual copies containing ignored .NET/TestResults output |
| CI | Fix branches lacked push checks; superseded runs could keep consuming workers | Add fix/** trigger, cancellation group, 15-minute timeout, validator regressions and a post-build hygiene check |
| Mission scope | This baseline's old mission still requested cancelled Free Time | Reapply day/night-only direction; add current working conventions, including owner-managed README files |

## Redundancy and cleanup choices

- Both audit reports were retained; the colliding `Docs/AUDIT.md` becomes
  `Docs/PRE_MERGE_AUDIT.md`, while `docs/AUDIT.md` remains at its old path.
  Architecture references follow the rename. Historical reports point here.
- The settled-text upload guard removes repeated native work without replacing TMP
  layout generation. Cache invalidation covers palette, policy, document and layout.
- Removed only the unused `SetSemanticColors` API after settings became its sole owner.
- Retained BasicNpcStrategy: it implements the strategy interface and has explicit
  swappability tests. It is an alternative policy, not proven dead code.
- Retained StarterWinCondition and CapabilityWinCondition: the factory selects between
  their distinct supported catalogues; deleting either would alter legacy behavior.
- Retained Trial Mode, explicit role slots, rounded-surface generation and existing
  typed DTOs. They remain referenced by runtime, authored content or tests.
- Avoided broad formatting/renaming churn and scene/prefab rewrites. Existing metadata
  GUIDs and serialized field names remain intact. Reintroduced helper metadata uses
  its previously committed identity.

## Remaining issues and limits

1. Mission role rules, win stealing/immunity/backlash/promotion and setup controls are
   missing, not fixed by this cleanup. Existing legacy outcomes are not mission outcomes.
2. Claim/bluff permissions are absent from RoleSpec; ordinary Town can claim another
   role or report a made-up alignment. This is a mission gap requiring the shared
   legality/data/UI contract, not an isolated cosmetic fix.
3. UI/narrator/night-report/menu copy remains hardcoded. Only the 70 spoken variants
   are externally authored. The earlier external-copy implementation is absent.
4. The old full-transcript TMP mesh grows with the run and is rebuilt per appended
   observation; choice buttons are recreated on menu navigation. Static per-frame
   uploads were reduced, but a bounded current-line/history UI remains needed.
5. Settings still use PlayerPrefs version 1, validate by falling back wholesale,
   and save synchronously on each control change. Versioned JSON migration/clamping,
   debounced persistence and full game/display/accessibility setup remain unfinished.
6. Physical Space-key polling remains; no remappable action asset/current-line pointer
   handler is wired. Menu focus/layout, touch/gamepad and modal behavior need native checks.
7. Per-frame UI rendering cost, readable scaling and actual TMP baseline behavior have
   not been profiled or rendered here. The IMGUI playground still approximates TMP.
8. Full mission dialogue metadata, coverage/near-duplicate tooling, archetype voices,
   stats/trait gates, explicit reasons/truthfulness, signatures and volume are absent.
9. No native package resolution, Unity compilation, editor/build callback execution,
   PlayMode rendering/input, profiler capture or player build has been run here.

## Verification

Local commands:

```sh
python3 Tools/validate_scaffold.py
python3 -m unittest discover -s Tools -p 'test_*.py'
git diff --check
```

Four validator tests exercise clean/copied repositories, ignored build output,
case collisions/conflict markers, missing serialized references and missing metadata.
Shared C# checks run through GitHub Actions with warnings treated as errors, plus
NUnit 3.5/.NET Framework compatibility compilation. PostMergeSmokeTests runs 16
seeds for each of all 11 selectable roles, in both supported voting modes: **352
complete games**, plus repeated seed-1 comparisons, using real authored dialogue,
public choice IDs, stale-input rejection and bounded termination. This is a smoke
regression, not the requested AI-only thousands-game balance baseline.

Final CI receipt will be recorded in the PR. Separate native regressions cover the
actual settings button path, EventSystem reuse and policy-gated TMP geometry, but
remain unrun until Unity is available.

## Native Unity checklist

1. Open Unity 6000.3.26f1; resolve the pinned Input System 1.17.0, uGUI 2.0.0 and
   Test Framework 1.6.0 packages. Enable the Input System and import TMP resources.
2. Compile all assemblies; run EditMode and PlayMode tests, including settings-button,
   stale-motion-frame and EventSystem-reuse regressions.
3. During queued speech, change speed, motion, flashes and intensity. Verify no queued
   line is skipped and disabling motion immediately restores baseline geometry.
4. Test bootstrap with no EventSystem, one compatible EventSystem, and deliberately
   incompatible/duplicate systems. Verify reuse and actionable errors.
5. Resize/reflow text after reveal and replace fear/accusation text. Confirm the dirty
   guard preserves visible text, palette changes and animation without drift.
6. Validate all content through the menu; try invalid markup, a dangling role reference
   and a valid custom setup excluding some catalogue roles. Restore each test edit.
7. Attempt an actual Unity player build with invalid then valid content. Confirm the
   native build gate; record the build result separately from portable CI.
8. Play through discussion, response, tie/revote, one elimination, night, morning,
   player death/spectating and restart in both voting modes. Check a peaceful morning
   has no death flash. Test keyboard/mouse/touch/gamepad and narrow resolutions.

## API references checked

- [Unity 6000.3 build preprocessing](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/build/ipreprocessbuildwithreport/onpreprocessbuild)
- [Unity object discovery and inactive objects](https://docs.unity3d.com/kr/6000.0/ScriptReference/FindObjectsInactive.html)

See `MISSION_PROGRESS.md` for the mission assessment against this actual tree.
