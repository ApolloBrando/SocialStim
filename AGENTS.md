# SocialStim working conventions

- Gameplay has only day (discussion/voting) and night (role actions). Free Time is cancelled; see MISSION_SCOPE.md.
- Before starting a phase, inspect relevant existing branches and open PRs. Account for overlapping implementations rather than assuming main contains all work.

- Prefer Unity-native APIs, components and editor tooling wherever they improve authoring, input, rendering, validation, profiling or maintenance. Evaluate this deliberately when adding or replacing custom infrastructure.
- Keep authoritative rules, inference and seeded randomness in plain C# behind the existing assembly boundaries. Unity adapters must not make game decisions or replace IRandomSource with UnityEngine.Random.
- Inspect ProjectSettings/ProjectVersion.txt and Packages/manifest.json before selecting APIs. Verify unfamiliar APIs against official documentation for those versions.
- Keep dialogue and displayed game wording in editable data. Use stable content keys in code.
- Work on feature branches with focused commits and PRs. Never commit or merge to main, force-push or rewrite history. Preserve existing asset GUIDs and serialized fields; include meta files for new assets. Document deletions.
- Unity is unavailable in this agent environment. Never describe headless checks as Unity compilation, rendering, profiling, editor tests or player builds. Document pending Unity verification.
