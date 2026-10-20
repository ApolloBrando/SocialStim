# SocialStim working conventions

- Use the current main branch as the baseline. Inspect its actual files and history; a merged child PR does not imply its changes reached main.
- Do not edit README files. The owner is maintaining them.
- Gameplay has day (discussion/voting) and night (role actions). Morning reports, last words and elimination are transitions; Free Time/social visits are cancelled.
- Keep displayed game wording in editable data. Existing hardcoded copy is unfinished mission work, not a precedent for new hardcoded product wording.
- Prefer Unity APIs and editor tools where they improve authoring, input, rendering, validation or maintenance. Keep authoritative rules and seeded randomness in portable C# behind the assembly boundaries.
- Work on feature branches and submit focused PRs; never commit or merge to main, force-push or rewrite history. Preserve the owner's concurrent changes.
- Preserve existing serialized fields and asset GUIDs. Include metadata for new assets and document removals.
- Run the Python scaffold checks and validator tests, the shared C# tests and NUnit compatibility compilation. Unity is unavailable here: never describe portable checks as Unity compilation, native tests, profiling or player builds.
