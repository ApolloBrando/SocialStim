# Repository audit

Base: main at 4d94dabafa292ecea50fe674170fa20660219082.

## Scope

Reviewed the Rules, Application, Data, Presentation and Editor code, test
fixtures, authored catalog/role/setup/dialogue assets, package/settings pins,
validation tools, CI configuration and project documentation. This is a source
review with targeted regression checks; Unity import and visual interaction
still require the editor.

## Findings addressed

| Finding | Change |
| --- | --- |
| Main CI failed the Provocateur execution scenario | The fixture used a one-day cap while expecting play to continue after a day-one execution. Test with two days; assert faction play continues and the personal achievement survives to the actual terminal result. |
| Fixed 20-participant / 64-role limits | Remove the upper bounds; scale faction slots from an inspector participant count and authored shares. Validate names, chosen roles and uniqueness. |
| Recursive slot search could exhaust the call stack once cast limits were removed | Use bounded iterative backtracking, including a 4096-slot regression. |
| Two independent assignment algorithms | Route the seven-person fallback and authored/scaled setups through RoleList.Resolve. |
| Horizontal participant and claim rows could overflow | Use horizontal scroll strips with stable card widths. |
| Callback could synchronously submit a second command mid-turn | Reject reentrant submissions; retain finally-based release of the guard. |
| A mismatched role faction/category could select an unintended personal objective | Reject mismatched definitions before assigning roles. |
| Scenario names/traits and null inputs failed late | Validate identities, personality flags and construction inputs before building a session. |
| Dialogue variable typos containing underscores escaped validation | Validate every braced variable against the whitelist. |
| Metadata values lacked strict string type checks | Reject malformed values with the line ID. |
| Optional-target conditions could contradict each other | Reject true/false presence contradictions during import. |
| Action reports repeated the speaker prefix | Keep attribution in RunSession, remove it from authored report wrappers. |
| Action reports exposed raw ability enum wording; result wording was duplicated | Centralize natural first-person selection templates and reuse result descriptions for private/public reporting. |
| Identical agreement/disagreement/report variants and placeholder noise | Replace with 70 distinct editable lines, four generic variants per intent plus contextual variants. |
| Internal phase enum names and lengthy interface text appeared in play | Use brief announcements/choices and omit redundant execution/resolution/end transitions. |
| Day warmth assumed three discussion rounds | Pass the configured length through the player view. |
| Effects Playground still reversed day/night colors | Align preview direction with gameplay. |
| An imported intermediate intensity could make the next increment exceed one | Clamp the increment before constructing validated settings. |
| Authoring and verification docs contained obsolete placeholder/save-branch references | Add CONTENT_GUIDE and refresh the affected documentation. |

BasicNpcStrategy remains as a documented injectable comparison strategy, not
an unused duplicate to remove. Placeholder strings inside artificial test
fixtures are test data; production starter dialogue has none. Existing Unity
asset GUIDs are preserved.

## Checks and limits

The initial main run compiled against NUnit 3.5 and ran 107 tests with NUnit
3.14: 106 passed, the fixture above failed. The audit adds population tests
through 32 participants, an authored cast beyond 20, a catalog beyond 64,
chosen-role/uniqueness/name checks, callback reentry and authoring regressions.
CI results for this branch are recorded in its pull request.

Local command execution is unavailable in this session. GitHub Actions runs
the scaffold validator and both shared-code matrix checks. Those checks exclude
Unity-facing Data definitions, Editor and most Presentation adapters. Follow
UNITY_VERIFICATION and EFFECTS_VERIFICATION for editor import, TMP rendering,
horizontal scroll behavior and mouse/keyboard testing.

Population flexibility does not guarantee balance or performance at arbitrary
sizes. Constraint search is bounded and fails clearly for incompatible setups.
The default catalog provides 20 names; add names before requesting a larger
cast. No Windows player build or Unity profiler result is claimed.

Verified code commit: f6f2b7a76e7bb9799281b6dc72edb06510d0a439.
GitHub Actions run 39295451222 passed the scaffold validator, NUnit 3.5 API
compilation and all 124 NUnit 3.14 behavioral cases. No Unity checks ran.


## Follow-up checkup — 2026-10-21

Baseline: current main `a1f1495` (merged daily-trial fix, source consolidation and stale-chart-test replacement). This pass reviewed phase/input transitions, all night-stage ordering and role resources, claim/report legality, observer-local evidence, NPC strategies, report composition, presenter lifetime, roster projection, TMP/effects source, content loaders and editor/build validators. Repository/package/assembly/GUID hygiene is checked by the scaffold validator. The historical sections above describe their original baselines.

### Fixed issues

- **Impossible or mechanically empty NPC cover reports.** A passive role previously always reported passing; a self-protecting role could reuse itself as a Sheriff/Physician/Lookout/Vigilante target. Automatic NPC covers now preserve a valid real visit when possible, otherwise select a plausible other night participant from the observer's own opinions and public identities. This does not read another actor's hidden orders/results. Honest Town reports still use the original recorded action. Player-authored report choices remain explicit.
- **Legacy report bypass.** `Speak(ReportInvestigation, ...)` previously bypassed the public-claim requirement and did not consume the once-per-night allowance. It now requires the same claimed investigation role, permits only truthful Town results or authorized bluffing, and publishes through the structured report path. Repeating it or mixing it with Give Report cannot publish twice. The unused old report-prose parameter has been removed from the speech helper.
- **Unanchored agreements.** Agreement/disagreement could refer to yesterday's statement even though the UI's reply context is limited to today. Session legality, belief updates and NPC selection now agree on the current-day boundary. Historical evidence remains available for deduction; an unsupported reply cannot create new trust or suspicion from an old statement.
- **Disabled cover roles.** A Town role explicitly marked nonclaimable could still be offered to bluffers, even though fabricated-report validation rejected it later. Shared claim permissions now honor the authored claimability flag for covers too.
- **Presenter subscription leak.** If the initial view render threw during presenter construction, input stayed subscribed to an otherwise abandoned presenter. Construction now validates its ports and removes the subscription before propagating a render failure.
- **Inconsistent roster abbreviations.** Claimed roles now use editable `role.short.*` entries, matching the spectator list, with the existing fallback for custom roles.

### Verification and limits

The initial targeted run reproduced 16 failing cases before the fixes; the self-protection/passive-role matrix was then expanded. All **424 shared C# tests** pass, including **20 new regression cases**, 1,100 scoped seeded benchmark games, 176 full-catalogue games in each voting mode, and the existing all-role/knowledge-boundary tests. These simulations check legality, repeatability and progress; their outcome counts are not a balance or human-quality AI claim.

Python scaffold validation and all four validator tests pass. NUnit 3.5/net472 compatibility compilation passes with zero warnings/errors. A temporary Roslyn C# 9 syntax parse covers all 151 source files under both editor and player preprocessor configurations with zero syntax errors. This is not Unity type compilation.

Unity is unavailable: native import, editor/PlayMode execution, actual profiling and player builds were not run. In Unity, verify restart after a render failure, claimed-role abbreviations, same-day reply context and neutral bluff reports alongside the existing nightly-role smoke checks. Existing hardcoded UI/event copy remains incremental mission work; this pass adds no hardcoded player-facing wording. No README edits, new source assets, GUID changes, file removals or package changes.
