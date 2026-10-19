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
