# Unmerged branch consistency review

Reviewed 2026-10-20 against remote refs fetched that day. Main snapshot:
`ab8f56726f9cdd794cdcc944620c8c181bf285e0`. This is an inspection, not a merge.

## Verdict

The parent chains are coherent within each implementation stack. The two stacks
are **not ready to merge together**. They implement overlapping settings,
bootstrap, role contracts, dialogue and rendering APIs differently. Independent
passing CI does not demonstrate that a combined game compiles or behaves correctly.

Both current follow-up tips agree on day/night-only gameplay. The council follow-up
removes the actual Free Time implementation; the recent stack had no corresponding
runtime feature. Historical parent heads still contain cancelled requirements or
code until their follow-up is included.

## Inventory

All 14 remote branches not merged into main were considered. SHAs below identify
code snapshots before this report-only commit. PR numbers refer to open draft PRs.

| Branch | Head | PR | Relationship and purpose |
| --- | --- | --- | --- |
| `phase/0-audit` | `63f8704` | #8 | Older stack root, predates current main's audit/discussion work |
| `phase/1-core-roles` | `83dc33f` | #9 | Follows #8; mission role mechanics |
| `phase/2-council-interaction` | `e140f0f` | #10 | Follows #9; CouncilSession and council UI |
| `phase/3-settings-customization` | `9cea286` | #11 | Follows #10; full setup, stats and settings |
| `phase/4-inference-social-ai` | `60885e5` | #12 | Follows #11; inference, social AI and simulation |
| `phase/7-remove-free-time-council` | `d519939` | #21 | Follows #12; removes cancelled gameplay |
| `phase/2-settings` | `48d5dad` | #16 | Recent stack root; RunSession settings |
| `phase/3-expanded-effects` | `4f3e4f3` | #17 | Follows #16; expanded text effects |
| `phase/4-interaction-ui` | `18b964f` | #18 | Follows #17; revised interaction UI |
| `phase/5-dialogue-reference` | `26e9c9b` | #19 | Follows #18; editable text and complete catalogue |
| `phase/6-unity-workflow` | `6d1f571` | #20 | Follows #19; Unity authoring, input and validation tools |
| `phase/7-day-night-only` | `ba365f6` | #22 | Follows #20; matching mission scope and this review |
| `phase/5-dialogue-cast` | `60885e5` | None | Exact same commit/tree as #12; no independent dialogue work |
| `milestone/04-save-resume` | `7e827c5` | Closed #6 | Previously cancelled save/resume work; excluded from integration |

Do not treat intermediate stack PRs as independent alternatives or merge every
branch indiscriminately. The duplicate dialogue-cast ref adds nothing. The closed
save/resume branch should not be resurrected by this audit.

## Reproducible merge checks

Read-only `git merge-tree --write-tree` checks used remote refs. They did not change
a branch, index or working tree. Exit status 1 means unresolved conflicts.

| Combination | Conflicting paths | Synthetic tree |
| --- | ---: | --- |
| Council follow-up + recent follow-up | 13 | `571e6e2bb51710fea873aa5e79f7b25d8a2a06ec` |
| Main + council follow-up | 2 | `1e1256d13df4e479d7b9429ce39e3eb164f0c8e2` |
| Main + recent follow-up | 1 | `eb3840b4a13ab3f86b7ffaf66ee4caca0f95afa8` |

The cross-stack conflicts are:

- `ARCHITECTURE.md`
- `Assets/_Project/Application/PresentationCues/DisplayPlayback.cs`
- `Assets/_Project/Application/Settings.meta`
- `Assets/_Project/Application/Settings/GamePreferences.cs`
- `Assets/_Project/Application/Settings/GamePreferences.cs.meta`
- `Assets/_Project/Data/Definitions/RoleDefinition.cs`
- `Assets/_Project/Presentation/Effects/TextEffectRenderer.cs`
- `Assets/_Project/Presentation/Text/GameBootstrap.cs`
- `Assets/_Project/Presentation/Text/TextGameView.cs`
- `Assets/_Project/Rules/Dialogue/DialogueSelector.cs`
- `Assets/_Project/Rules/Roles/RoleSpec.cs`
- `Assets/_Project/Tests/PlayMode/TextEffectsAdapterTests.cs`
- `README.md`

Main versus council conflicts in `ARCHITECTURE.md` and `TextGameView.cs`; main
versus recent conflicts in `README.md`. Main already includes merged #13/#14,
so the older stack must account for that work when integrating.

The synthetic cross-stack tree also contains case collisions:
`Docs/AUDIT.md` / `docs/AUDIT.md` and `Docs/SETTINGS.md` / `docs/SETTINGS.md`.
Consolidate these with explicit case-safe renames before cross-platform checkout.
The two settings metadata add/add conflicts have different GUIDs: select canonical
identities deliberately and reconcile references; do not generate replacement GUIDs.

## Behavioral and API blockers

### 1. Competing runtime entry points

The council bootstrap defaults to CouncilScreen + CouncilSession, loads the mission
roles and exposes legacy RunSession as Trial Mode. The recent bootstrap starts the
reworked TextGameView + RunSession with the legacy role catalogue. Choosing either
bootstrap wholesale loses important work from the other stack. The integration
needs one deliberate default game path and explicit handling of Trial Mode.

Use the council rules as the mission-game foundation, then port the newer UI and
Unity workflow into its adapters. This is an integration recommendation, not a
completed port or permission to merge.

### 2. Settings classes and persisted schemas disagree

Both add `Application/Settings/GamePreferences.cs` with incompatible members.
The council stack uses AppPreferences, separate display/game preferences,
SettingsService draft/apply/cancel behavior, custom slots/roles, stats and difficulty.
The recent stack uses GamePreferences with EffectsSettings, NewRunSettings and
its display fields, PreferencesEditor and SettingsCodec.

The council bootstrap writes `settings-v2.json`; the recent implementation writes
`settings.json`. Both codecs use version 2, but their JSON layouts and models differ.
Neither is a migration for the other. A combined implementation needs one model,
explicit migration from both formats, and preserved custom setup and display values.
A clean text merge alone cannot resolve this.

### 3. Role and content contracts differ

Council RoleSpec carries RoleFlags, RoleObjective and three-way readout mechanics,
with explicit claim/bluff permissions. The recent contract adds nullable permission
defaults and a shared CanClaim helper without the full mission-role additions.
Preserve the actual mission mechanics while adapting the newer UI permission code.

Council GameCatalog has separate LoadRoles and LoadMissionRoles. Its LoadDialogue
validates role conditions against LoadRoles, while the game uses LoadMissionRoles.
The newer editor ContentValidation also validates LoadRoles. Mission-role-gated
content therefore needs validation against the runtime mission catalogue; merely
porting the editor tool unchanged leaves that coverage gap.

### 4. Effects APIs do not match council callers

CouncilDialoguePlayer calls TextEffectRenderer.SetSettings. The newer renderer
exposes SetPolicy and StopEffects and adds expanded glyph spans, lifetime behavior
and profiler instrumentation. Keeping the newer renderer requires adapting council
playback, settings preview and lifecycle calls, plus preserving both playback test
sets. DisplayPlayback is another direct conflict with consumers on both sides.

### 5. Dialogue overlaps; full authoring work remains

The council follow-up has 126 spoken JSON entries. The recent stack has 70.
All 70 IDs exist in the council data with identical rows; there are 56 council-only
rows and no recent-only spoken rows. Combining them yields 126, not 196. Preserve
the council additions and the recent external UI-copy templates/catalogue tooling.
The recent stack also has 303 separately keyed runtime-copy templates; those are
not 303 additional spoken dialogue choices.

CouncilSpeechService still constructs strings such as patient-attack reports in
C#, and council screen/session copy has not been ported into the recent text
catalogue. That violates the owner's current editable-text requirement if shipped
as the unified game. Move those strings to stable content keys and regenerate the
catalogue after the port.

The council speech service evaluates an action reason for social behavior, but
its DialogueContext does not carry that reason, stat bands or truthfulness to the
selector. The newer stack does not complete that mission dialogue contract either.
Large reason/stat-matched dialogue coverage remains unfinished; the dialogue-cast
branch is not evidence that it was delivered.

### 6. Unity workflow is not yet applied to the council path

The recent stack adds InputAction ownership, pointer handling, EventSystem reuse,
UI Toolkit authoring, import/build validation and profiler scopes. The older council
screen still uses its own keyboard polling/button flow. Port and verify these
adapters on the actual default council game, rather than assuming the newer
bootstrap's tooling automatically covers it. Keep authoritative rules and seeded
randomness in portable C#.

## Verification and limits

- Council follow-up CI run `39409671418`: **180 passed, 0 failed, 0 skipped**;
  its simulation receipt step also succeeded.
- Recent follow-up CI run `39409683143`: **188 passed, 0 failed, 0 skipped**;
  complete catalogue freshness check succeeded.
- Both runs also passed the older NUnit 3.5 compatibility compilation job.
- Local scaffold validation and whitespace checks passed on both follow-ups;
  catalogue freshness passed on the recent stack.
- Added council regressions cover direct discussion/vote/elimination/night/morning
  flow, rejected retired commands/intents, stable remaining enum numbers and ignored
  legacy settings keys. The four dedicated Free Time dialogue entries were removed.

These suites run separately and overlap. Do not add their test counts or call them
a passing combined suite. Unity is unavailable here: no native Unity compilation,
PlayMode execution, rendering/input checks, profiling or player builds were run.
The synthetic conflicted trees are diagnostic objects, not playable builds.

## Integration order to use for subsequent work

1. Start an isolated integration branch, retaining both reviewed tips and current
   main's changes. Keep the cancelled save/resume branch out.
2. Resolve the canonical council runtime and full role contracts first. Retain
   Trial Mode only through a deliberate supported route.
3. Unify settings and migrate both persisted formats; preserve chosen asset GUIDs.
4. Port expanded effects, UI, Unity input/authoring and validation onto council.
5. Union the spoken data by stable ID, externalize remaining council wording, and
   extend catalogue validation to the actual mission roles and supported variables.
6. Consolidate case-colliding documentation, then run the combined portable suite,
   inference simulations and native Unity validation before requesting a main merge.

No integration branch, main merge, branch deletion or history rewrite was performed
as part of this review. The identified incompatibilities remain open work.
