# PR handoff

The branches are stacked. Create them with the bases below, or target main after the preceding branch has merged. GitHub API access is blocked in this environment; all three branches have been pushed for manual PR creation. No main merge was performed.

## 1. Role rules and evidence

- Head: `fix/role-rules-and-evidence`
- Base: `main`
- Title: `feat: clarify roles and add guilt and Jester revenge`

Rename all requested displayed roles while preserving stable IDs and GUIDs. Add successful Town-kill Vigilante guilt on the following night, pre-night Jester revenge restricted to living Town guilty voters, one counterclaim per participant, public-evidence suspicion after a duplicate claimant's night death, and optional execution reactions. Standardize role references and extend authored-role edge tests. Existing fabricated nightly reports remain covered. No deletions or README changes.

See [ROLE_REVISION.md](ROLE_REVISION.md) for decisions, detailed coverage and the Unity checklist.

## 2. Separate QoL pass

- Head: `feat/qol-relations-and-spectator`
- Base: `fix/role-rules-and-evidence`
- Title: `feat: add spectator role labels and NPC relation chart`

Show abbreviated true roles beside names for spectators using a separate snapshot map, without publishing hidden roles to NPCs. Add a developer-only, directed NPC relation chart with suspicion, trust and faction estimates, viewpoint switching, target paging and native modal navigation. Update stale native UI tests to check the already-merged 0–200% slider and direct roster targets. Preserve character-versus-mention colours, quiet action colours, action underlines, branch context, wrapping and removed context options. No deletions or README changes.

See [QOL_UI_UX_RESEARCH.md](QOL_UI_UX_RESEARCH.md) for every QoL change, primary-source research, retrieval limitations and native acceptance checks. The existing twelve main-branch QoL improvements are explicitly distinguished from this follow-up.

## 3. Dialogue polish

- Head: `content/natural-trait-dialogue`
- Base: `feat/qol-relations-and-spectator`
- Title: `content: polish trait dialogue and execution replies`

Review the 970 existing records; rewrite 534 NPC and 40 player lines while preserving every old ID, metadata field, condition, weight and placeholder. Add 42 execution-specific, same-trait replies using the existing schema and phase predicates. Keep all seven voices distinct, reduce repeated self-commentary and formal phrasing, and update authoring/coverage documentation. The final catalogue has 1,012 unique lines. No deletions or README changes.

See [DIALOGUE_POLISH.md](DIALOGUE_POLISH.md) for editorial decisions, actual checks and manual reading/selection verification.

## Final combined checks

371 portable C# tests pass with zero failures/skips, including the 1,100-game benchmark and both voting-mode full-catalogue runs. Python scaffold/content validation and all four validator tests pass. NUnit 3.5/net472 compilation passes with zero warnings/errors. C# 9 syntax parsing passes for all 185 files. Unity 6000.3.26f1 native import, compilation, EditMode/PlayMode execution, layout, keyboard/pointer interaction and player builds remain unverified and require the documented editor checks.
