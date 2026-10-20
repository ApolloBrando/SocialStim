# SocialStim

A single-player, text-based social deduction game built with Unity and C#.
Inspired by Town of Salem, Gnosia and Slaughter Horse 2.

At least, in theory.

Mission part two is tracked in [the source audit and phase plan](Docs/AUDIT.md).
Phase 0 fixes are on `phase/0-audit-fixes`; Unity verification remains pending.

The [Phase 1A discussion/voting foundation](Docs/DISCUSSION_VOTING.md) adds
configurable group rounds, response windows and plurality ballots on its feature branch.

Settings now use immutable preferences, versioned JSON persistence and a grouped
modal with immediate display edits and next-run game configuration. See
[settings contracts and Unity checks](Docs/SETTINGS.md).

The [expanded span effect system](Docs/EXPANDED_EFFECTS.md) adds 32 implemented
glyph behaviors, four lifetimes, nested validated authoring and semantic recipe assets.
Unity adapter rendering/profiling remains pending.

The [interaction/UI overhaul](Docs/INTERACTION_UI.md) replaces menu hierarchies
with action → roster target → authored phrasing, a single current line, two-press
reveal/advance and an information drawer. Shared legality checks enforce role
claim permissions and actual honest investigation results. Unity QA remains pending.

All current dialogue and game copy is editable in JSON. Use the
[complete dialogue catalogue](Docs/DIALOGUE_CATALOGUE.md) to browse every entry
and the [editing guide](Docs/DIALOGUE_GUIDE.md) to change it. Narration, result
wording, choices and interface copy have no hardcoded prose fallback.

The [Unity workflow pass](Docs/UNITY_WORKFLOW.md) adds native dialogue editing,
automatic content/import/build validation, role/catalog inspector feedback,
InputAction-based text controls and TMP profiler scopes. The Unity-first working
preference is recorded in [AGENTS.md](AGENTS.md). Native verification remains pending.

The [current gameplay scope](MISSION_SCOPE.md) is day discussion/voting and night
role actions only. The previously proposed Free Time activity is cancelled.
