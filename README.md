# SocialStim

A single-player, text-based social deduction game built with Unity and C#.
The player belongs to the cast and can receive any supported role, including a
hidden antagonist. Each seeded run has a new cast, personalities, and role setup.
There are no time loops, cross-run knowledge unlocks, or runtime generative AI.
All final dialogue is authored by the project owner.

## Current status

Milestone 00 provides the project foundation: folder structure, assembly
boundaries, Unity configuration, source-control hygiene, and design documents.
There is no gameplay code, scene, input wiring, authored dialogue, or playable
build yet. Pressing Play does not start a game.

The source repository was empty at inspection. This test project permits direct
publication to `main` after the owner confirms the concrete batch of changes.
Initial publication creates a bootstrap README commit through GitHub's contents
API, followed by the reviewed project files. Until publication is confirmed and
verified, the prepared local branch is `milestone/00-project-foundation`.

## Pinned environment

| Component | Version / decision |
| --- | --- |
| Unity Editor | **6000.3.26f1**, Unity 6.3 LTS |
| Initial target | Windows desktop; keyboard and mouse |
| Render pipeline | Built-in initially; no URP dependency |
| Runtime UI | uGUI 2.0.0, including TextMeshPro |
| Input System | 1.17.0 |
| Unity Test Framework | 1.6.0 |
| Content loading | Inspector-referenced catalog; no Addressables yet |

These are implementation defaults from the accepted architecture proposal.
The editor release is recorded in `ProjectSettings/ProjectVersion.txt` and
direct package versions in `Packages/manifest.json`. This environment has no
Unity editor: package resolution, script compilation, and scene import have
**not** been executed. No transitive package lock has been fabricated.

## Open and verify

1. Install Unity **6000.3.26f1** through Unity Hub. Use the exact version rather
   than accepting an automatic editor upgrade.
2. Add/open the directory containing `Assets`, `Packages`, and `ProjectSettings`.
3. Let Unity resolve packages and generate missing default project settings.
   This is a minimal authored scaffold, not an editor-generated template export.
4. Confirm **Edit > Project Settings > Editor > Asset Serialization** is
   **Force Text**, and version-control metadata mode is **Visible Meta Files**.
5. Review and commit `Packages/packages-lock.json` plus editor-generated
   `ProjectSettings` files on the feature branch after a successful import.
   Do not commit `Library`, `Temp`, `Logs`, or `UserSettings`.
6. Confirm package versions in Package Manager. TextMeshPro is supplied through
   Unity 6's uGUI package; no separate legacy TMP package is declared.
7. Run `python3 Tools/validate_scaffold.py` from the repository root.

Empty assembly definitions can produce Unity warnings that an assembly has no
scripts. This is expected at milestone 00; runtime code and real tests arrive
in milestone 01. There are no scene components or inspector references to wire
at this stage. Input System activation and TMP essential resources are setup
work for the first UI scene, not completed work in this milestone.

## Architecture

- `Rules`: deterministic C# game behavior; no engine references.
- `Application`: session orchestration, observations, input requests, saves,
  and semantic presentation cues; no engine references.
- `Data`: Unity-authored definitions and validated dialogue data.
- `Presentation`: uGUI/TMP views, device input, and composition adapters.
- `Editor`: authoring/import tooling, excluded from player builds.

Rules and Application cannot reference Presentation, Unity scene objects, or
UnityEditor. Presentation does not receive unrestricted hidden state.
See [ARCHITECTURE.md](ARCHITECTURE.md) for dependency direction and planned APIs,
[Docs/GAME_DESIGN.md](Docs/GAME_DESIGN.md) for the starter rules and roles, and
[Docs/MILESTONES.md](Docs/MILESTONES.md) for delivery gates.

## Validation

`python3 Tools/validate_scaffold.py` checks the assembly graph, Unity metadata,
JSON syntax, version pins, text hygiene, and prohibited project state. It is a
static scaffold check, not a replacement for Unity import or game tests.

After milestone 01 adds meaningful tests, run them from Unity's Test Runner or
the command line. For example, with the editor executable already on PATH:

```sh
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile TestResults/editmode.log
```

Create `TestResults` first. Do not interpret an empty test suite as a passing
gameplay implementation. No Unity tests are present in milestone 00.

## Repository practice

Use small Conventional Commits, asset/`.meta` pairs, and updated README/architecture
documentation. Feature branches and PRs remain the normal workflow; this test
project also permits a direct `main` publication after confirmation of that
specific batch. Do not merge without authorization, rewrite history, delete
files, or commit secrets. See [CONTRIBUTING.md](CONTRIBUTING.md).

No license has been selected; this scaffold does not grant a license.
