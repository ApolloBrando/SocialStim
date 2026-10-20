# Mission part two: source audit

Baseline: `main` at `fc8f5cde97f1d2acba26e2e54a218179604ad153`.
File/line references below describe that baseline, before the fixes in PR #13.
This file uses the repository's existing `Docs` directory; introducing a second
`docs` directory would create case-sensitive/case-insensitive checkout problems.

## Scope and evidence

The baseline inventory read all 424 tracked UTF-8 files: 144 C# files, seven
assembly definitions, 204 metadata files, 18 Unity asset/settings files, one
scene, two JSON files, 16 Markdown files, the mission, and repository tooling.
Source review follows the runtime, data, presentation, editor and test boundaries
below. Metadata was checked structurally rather than treated as executable code.
All 33 GUID references in scene/settings/definition assets resolve to committed
metadata. There are no committed prefab files or external binary assets.

Unity is unavailable. Source inspection does not verify visual overlap, TMP
rendering, package resolution, adapter compilation, input devices or profiling.
The local environment has Python and Git but no `dotnet`; shared compilation
and NUnit execution use GitHub Actions. Historical results in other documents
apply only to their recorded commits.

## Architecture and data flow

| Assembly | Key types and responsibility | References |
| --- | --- | --- |
| Rules | RunFactory, RoleList, RunSession, PhaseMachine, NightResolver, BeliefState, strategies, DialogueSelector, win policies | No Unity or project assembly references |
| Application | GameController creates revision-scoped PlayerInputRequest/PlayerOption; DisplayPlayback and VisibleCueResolver translate permitted observations into presentation timing/cues | Rules only; no Unity |
| Data | GameCatalog, RoleDefinition, RoleListDefinition, ReasoningDefinition copy Unity assets into plain records; DialogueDocument/DialogueSchema import JSON | Rules and Unity |
| Presentation | GameBootstrap composes a run; GamePresenter routes input; TextGameView builds UI and queues observations; TextEffectRenderer owns TMP geometry | Rules, Application, Data, uGUI, TMP, Input System |
| Editor | ContentValidation and EffectsPlaygroundWindow | Rules, Application, Data; Editor only |
| EditMode tests | Shared rules/application/importer scenarios | Rules, Application, Data; Editor test assembly |
| PlayMode tests | Presenter port and actual TMP/UI adapters | All runtime layers plus uGUI/TMP |

Input travels from InputSystemUIInputModule/Button through the presenter and
controller to RunSession.TrySubmit. The controller rejects stale option IDs;
the session validates commands before changing state or consuming randomness.
NPC strategies receive KnowledgeView, never Participant or NightContext.
Public speech updates observer-local evidence before publishing a scoped
GameObservation. Night stages use authoritative state internally; private
reports reach only their living recipient. PlayerView carries public identities,
authorized teammate relationships, the player's role/results and their own
model estimates. Display acknowledgements release input but do not execute
rules, roll randomness or evaluate victory.

The assembly boundaries are sound. Separation inside the assemblies is weaker:
RunSession owns too many policies, and role growth requires edits to enums,
legality, strategy switches, report text and victory logic. This does not meet
the new requirement that adding a role should avoid core-system changes.

## Confirmed bugs and fixes

| Priority | Baseline file and line | Root cause and consequence | Disposition |
| --- | --- | --- | --- |
| P1 | Rules/Reasoning/SocialNpcStrategy.cs:86,97,132; BasicNpcStrategy.cs:44,54,74 | Historical suspicion can outweigh a hard known Town alignment. Town can nominate, convict or shoot someone its allowed knowledge confirms as Town. | Fixed in both strategies: confirmed alignment vetoes Town hostile selection. Tests cover both implementations. |
| P2 | Application/PresentationCues/DefaultCueProfiles.cs:16; EffectProfile.cs:43; DisplayPlayback.cs:47-60 | Shake is a 0.45-second pulse; its track expires even while a long line reveals. Completion also zeros channels and stops elapsed time, freezing any residual animation. | Explicit continuous text-motion tracks survive reveal/skip and tick until replacement/cancel. Continuous screen/flash tracks are rejected. Sequence composition preserves the flag. |
| P2 | Presentation/Text/TextGameView.cs:159-168,333 | Footer visibility uses hard-coded indices 2-6 and excludes Speed at index 7. Expanded controls compete for the same single footer row. | All six display settings now live in one separate panel; speed hides with it. Added Reset display defaults and a real uGUI regression test. |
| P2 | Editor/EffectsPlayground/EffectsPlaygroundWindow.cs:90-96 | Repaint stops once Busy becomes false, so continuous post-reveal animation cannot be inspected. | Preview continues repainting its retained document. |
| P2 | Presentation/Text/TextGameView.cs:311-318 | A policy change can leave retained motion using the previous immutable settings record. | Changing settings clears retained motion after completing the pending batch; the next line uses the new policy. |
| P3 | The Mission part two.txt, final line | Missing final newline makes the existing repository validator reject the user-added brief. | Appended one final newline; instructions unchanged. |
| P3 | .github/workflows/validate.yml:5 | New phase branches do not match milestone-only push triggers. | Added phase/**; pull-request checks remain enabled. |

The motion fix is the existing cue-driven lifecycle repair, not the full future
span-effect system. Semantic tags currently select colors; they do not yet own
independent continuous effect stacks. Those are tracked under the effects phase.

## Interaction, UI and settings audit

| Location | Finding | Planned replacement |
| --- | --- | --- |
| GameController.cs:34-57; TextGameView.cs:400 | Cartesian products of intent, target, result and role become category/family/page menus. Navigation can exceed the new three-step limit. | Chat/Accuse/Defend, roster target, up to three reason-matched lines; contextual Claim/Respond and Pass. |
| RunSession.cs:94-121; PhaseMachine | Three discussion rounds by default, fixed sequential NPC speeches, then strict-majority nomination and a trial. NPCs do not interrupt/respond in genuine exchanges. | Five discussion rounds, explicit player windows, response queue, plurality vote reveal and one revote. Retain trial as an optional policy. |
| RunSession.cs:164-180; GameController.cs:92 | Any role can claim any catalog role and fabricate an alignment report. No CanClaimTruthfully/CanBluff fields exist. | Shared legal-action builder, per-role permissions, actual result choices for honest claims, fabricated assertions only for permitted bluffers. |
| TextGameView.cs:74-81 | Claim board retains only the latest role string per person. It omits dated results and previous claims. | Public chronological claims ledger; no automatic contradiction solution in normal UI. |
| TextGameView.cs:95-155 | Always-visible title, status, roster, claim strip, transcript heading and choice heading divide focus. | Single current speaker/line and compact roster; ledger/history/help/settings in one drawer. |
| TextGameView.cs:311-318 | Changing a setting skips the entire pending batch. Applying display policy unexpectedly consumes unread presentation, although it does not submit a game command. | Live policy update without advancing the dialogue cursor. |
| TextGameView.cs:292-297 | Space/Skip consumes every queued observation. No first-click reveal / second-click advance contract exists; pointer click on text does nothing. | Reveal current line, then advance; optional auto-advance with reading delay. |
| UiPreferences.cs:11-23 | Invalid speed/intensity causes every preference to reset; no per-field clamping, version migration, individual reset, or JSON file. Stored speed remains the user's old choice after a new default ships. | Versioned settings file, independent validation/clamping, migrate supported PlayerPrefs, explicit per-setting/global reset. |
| GameBootstrap.cs:18-23; ShowSetup | Most game configuration is inspector-only; no player count/role-list/difficulty/stats setup UI. | New Run configuration with live legality checks before Start. |
| TextGameView / ChoiceFocusIndicator | No modal focus containment, Escape/back policy, controller verification or touch-size/layout verification. | Drawer/screen focus scopes and responsive layout tests; editor/device checks remain required. |
| GameBootstrap / GameController terminal request | No main menu, vote-reveal screen, Free Time, detailed morning screen or after-action report. Terminal state merely has no choices, with New run remaining in the header. | Explicit screen state and scoped after-action record. Continue must not imply a saved-run system: ongoing-run saving was previously cancelled. |

Current default text speed is **28 text elements/second**, multiplied by a stable
speaker tempo; speech holds are **0.65-1.64 seconds**, system holds 0.3 seconds.
This PR changes the default to **84**, speech holds to **0.20-0.54 seconds** and
system holds to 0.1 seconds. Existing valid saved speeds remain respected; Reset
display defaults selects 84. Instant already exists. The old speed button cycles
18/28/40; the temporary replacement offers 28/84/168 pending a full slider.
There are no punctuation-specific pauses to shorten in the current code.

## Redundancy, complexity and filler inventory

- RunSession mixes phase orchestration, ballots, team planning, speech legality,
  claim reporting, narrative wording and personal outcomes. Extract discussion,
  voting, claim-policy, report-format and outcome services as those systems change.
- TextGameView mixes setup, layout, settings, transcript accumulation, claims,
  presentation clock and menu pagination. Split screen/view adapters; do not grow
  it into the new UI's application state machine.
- BasicNpcStrategy is referenced by tests and supported injection. It is not dead
  code. Keep it until the replacement strategy and comparison tooling supersede it.
- StarterWinCondition is used for small catalogs. CapabilityWinCondition has an
  interface overload that always throws, and RunSession special-cases its concrete
  type. Replace with one typed outcome context; neither policy is safely unused.
- Ability dispatch is duplicated across NightLegality, SocialNpcStrategy,
  BasicNpcStrategy, ActionLabels, NightActionText and RunSession.DescribeReport.
  Introduce explicit ability/target/report handlers, not reflected discovery.
- TextGameView._menuCategory/_menuFamily/_menuPage and PlayerOption.Category/Family
  become redundant only after the new action flow is wired. Do not delete early.
- Empty .gitkeep files preserve folders and are ignored by Unity. Test-only
  PLACEHOLDER text deliberately supplies artificial fallback fixtures. Neither is
  production filler to remove as though it were an unused script.
- Setup's "Listen closely. Choose who to trust." and "SETTLE IN" add no controls
  or game facts. Move control instructions into Help and remove the generic opener.
- "CONVERSATION", "YOUR TURN", "Listening…", "Choose", "Keeps your turn.",
  "Continue the run.", and repeated menu choice counts largely restate UI state.
- "Discussion begins.", "Nominations open.", "Time to vote.", "Last words."
  duplicate phase headings. Replace with screen transitions unless narration adds
  an actual fact. Keep night/day numbers, ballots, result notices and death facts.
- Four silence variants (listens/stays quiet/says nothing/waits) consume full
  transcript entries/reading holds. Represent pass compactly in history.
- Starter.VoteJustification.001-.004 assert no actual reason ("I know my vote
  matters", "It was the best call I could make"). Replace with reason-tagged lines.
- Starter.Deflect.004 claims "We've barely heard from {target}" without a silence
  predicate or declared lie. The future AI/content contract must bind its reason.
- Generic reveal reactions ("That changes things", "Now we know a little more")
  need context or character voice; duplicate question/defense hedges need distinct
  voices rather than hundreds of vocabulary substitutions.
- No entire source files are deleted in this PR. Removed snippets are the old
  footer visibility loop, the old speed-cycle expression, and the old completion
  path that reset/stopped all text motion. The PR enumerates these deletions.

## AI and information limitations

BeliefDistribution.cs:44-80 normalizes independent faction weights and spreads
them across roles. It does not sample consistent joint worlds, respect a full
slot posterior or implement the mission's inference engine. BeliefState.cs:49-74
uses credibility/traits for accusations but still grants flat self-defense and
agreement adjustments. No per-run six-stat model or separate affinity exists.
Role reveals reconsider accusations and judgements, but not all nominations,
defenses, night patterns or claim timing. Duplicate unique-role claims do not
create a dedicated logical conflict. Team planning coordinates killers, not a
shared deception/block/forge plan. Night-target utility mostly sorts suspicion
or trust. There is no tally prediction, bounded-world inference, difficulty
budget, developer plan overlay or thousands-of-runs balance report.

Public setup counts are announced and used in inference. Hidden-list mode does
not exist; simply hiding the UI would leak its counts through NPC behavior.
Keep visibility policy in the knowledge builder. The known-role permutation
tests cover initial observations, not every later inference path. Hard knowledge
must always derive from that observer's permitted facts, never true cast roles.

## Roles and unresolved policy changes

The baseline has 11 roles, including Citizen, Inspector, Interrupter, Assassin,
Forger, Coordinator, Survivor, Vigilante, Provocateur and Lone Killer. Their
semantics differ from the mission: forging forces Antagonist rather than inverts
a readout; healing grants reusable defense rather than negating one attack;
neutral killing wins while benign neutrals remain; parity waits for Town combat.
None of these older policies should be silently relabeled as the new roles.

Implementation decisions for the role phase:

- Append serialized enum values; preserve existing IDs/GUIDs and use explicit
  migration aliases where a definition is renamed. Do not reinterpret old assets
  by reordering enum members.
- Default Anarchy + Infinitum alone is a **stalemate draw**: night immunity and
  symmetric plurality voting prevent either from forcing the other's elimination.
  This preserves last-survivor wording rather than granting a false Anarchy win.
- Infinitum alone does not steal a vacuous 0 >= 0 parity check. A steal requires
  at least one living Evil member, no Anarchy, and the configured parity policy.
- Forge applies to Decryptor readouts, not Physician attack/heal notices; the
  requested "forging a Physician's result" edge case must prove it is unaffected.
- Resolve the complete death batch before promotion, victory or win stealing.
  A pending Nullifier backlash remains explicit state until its next-night cleanup.
- Free Time follows the core discussion/vote/night contract. Proposed optional
  flavor abilities remain deferred and off by default.

## Performance hazards

TextGameView.Pump (261-290) rebuilds the entire accumulated transcript string,
spans and grapheme boundaries for every new line, then ForceMeshUpdate and
ForceUpdateCanvases. TextEffectRenderer.Paint visits every visible character and
uploads all vertices/colors every LateUpdate even when unchanged. The cost grows
with history rather than the current line. New UI must isolate current speech
from history, cap visible glyph work and skip static uploads. Capture resizes
buffers at mesh/layout boundaries; these are distinct from steady-state work.
Knowledge() creates many LINQ lists and role distributions for every strategy
call; BeliefState.Evidence copies a full history on access and KnowledgeView
copies/filter it again. Cache detached snapshots per observer/revision and
budget inference work by difficulty. RoleList's backtracking has an attempt cap,
but large authored populations can still allocate large slot pools. Setup UI
will cap population to available curated characters.

## Missing tests and manual gate

Portable regressions added here cover continuous motion after completion/skip,
monotonic post-reveal animation time, exactly-once acknowledgement, cancellation,
replacement, reduced-motion suppression, rejecting continuous flash, new speed
default, and confirmed-Town selection in both strategies. A Unity-only UI test
checks visibility, reset values and the settings-change event.

Still required: new role/win edge matrix; claim permissions and honest-result
selection; configurable plurality/revote/abstention; settings JSON migration and
clamping; stats/affinity/influence; consistent-world inference and mid-run hidden
permutations; reason-matched phrasing; dialogue volume/near-duplicates/cooldowns;
all effect duration modes and span stacking; bounded glyph work; simulation
metrics and deterministic replay across difficulties. The existing 100 seeded
runs use a simple player policy; they are not thousands of autonomous games or
a balance measurement.

Manual Phase 0 checklist, all pending until actually run:

- [ ] Import phase/0-audit-fixes in Unity 6000.3.26f1 with TMP resources and Input System.
- [ ] Run EditMode and PlayMode tests, including DisplayOptionsTests.
- [ ] At 1280x800 and 1920x1080, open/close display options; all settings including
      Speed hide together, no footer overflow, keyboard focus returns to toggle.
- [ ] Set full motion, then issue an accusation. Shake continues through reveal
      and while the final line waits for input; next line/restart removes it.
- [ ] During retained motion, enable reduced motion or disable effects; verify
      immediate baseline geometry and no lingering screen pulse.
- [ ] Reset defaults: 84 speed, reduced motion on, flashes off, intensity 50%.
      Restart Play Mode and verify saved choices. Instant still reveals immediately.
- [ ] Preview accusation in Effects Playground past reveal completion and cancel.
- [ ] Profile long-history glyph updates; record actual GC/frame results rather
      than inferring zero Unity allocations from portable tests.

## Phase plan

Each row receives its own feature branch/PR; large dialogue work splits into
reviewable intent batches. Nothing merges to main automatically.

| Phase | Deliverable and gate |
| --- | --- |
| 0 audit/fixes | This source audit, confirmed bug regressions, manual checklist, current baseline inventory |
| 1 UI/interaction | Explicit discussion/vote screen state, Chat/Accuse/Defend target/phrasing, Respond, drawer, line-level advance; preserve optional working trial policy |
| 2 roles/customization | Core nine role definitions/abilities, permissions, readouts, backlash/promotion/steal, validated presets and custom slots, run stats/setup |
| 3 settings | Versioned JSON preferences, migration/clamping, live apply, resets, display scaling and navigation |
| 4 inference/social | Observer-only sampled worlds, reason/utility decisions, faction plans, affinity, bounded mistakes and difficulty budgets |
| 5 dialogue | Curated cast/voices, metadata and validator, >=3500 reviewed unique lines, >=500 player lines, coverage/authoring guides |
| 6 effects | Span lifetimes/stacking, semantic style data, >=25 effects, parser, safety, playground and Unity profiling checklist |
| 7 extras/balance | Stable-core extra roles, optional Free Time, debug overlay, thousands-of-games simulation report and tuning |

Planned scope is not marked implemented by this document. Current Unity pins
remain 6000.3.26f1 / Input System 1.17.0 / uGUI 2.0.0 / Test Framework 1.6.0.
