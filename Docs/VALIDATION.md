# Validation

## Milestone 05 branch

The first effects branch build compiled portable code with warnings treated as
errors and passed **58 tests, 0 failures, 0 skipped**, including the existing
100 seeded runs and a zero-allocation steady portable playback check:
[run 39257380492](https://github.com/ApolloBrando/SocialStim/actions/runs/39257380492).
The envelope update passed **61 tests, 0 failures, 0 skipped**:
[run 39258208133](https://github.com/ApolloBrando/SocialStim/actions/runs/39258208133).
Three new envelope cases pin the alternate pulse shapes. Unity-only adapter tests
are excluded from this count and remain pending.

The headless project additionally compiles the engine-free GamePresenter and
its input/acknowledgement integration test from the PlayMode folder. It does
not compile TextGameView, TMP geometry, UI overlays, editor tooling or assets.
The real TMP adapter tests are in Unity's PlayMode assembly and remain unrun.
Follow `EFFECTS_VERIFICATION.md` for the distinct Unity and allocation gates.

## Milestone 01 validation

## Unity NUnit compatibility correction

The owner reported CS1503 at DialogueAndKnowledgeTests.cs line 73 during actual
Unity import: a CharacterId was passed to an assertion overload expecting string.
The original .NET suite used NUnit 3.14.0 and missed this Unity API difference.
Both collection assertions now use Has.No.Member. CI compiles upstream NUnit 3.5.0
on .NET Framework 4.7.2 as an older API baseline and runs 3.14.0 on .NET 8; neither is a substitute for Unity's
custom integration. The project also validates that the new Input System is
enabled before reporting starter content as ready. Editor compilation and
scene checks remain pending until the owner reruns them after this fix.

The first feature-branch GitHub Actions run compiled shared C# code and passed
33 NUnit tests with zero failures/skips, including 100 complete seeded runs
across Town and antagonist player roles:
[run 39247980079](https://github.com/ApolloBrando/SocialStim/actions/runs/39247980079).
Subsequent importer checks add a test against the actual 45-line starter file;
the latest PR check is authoritative for the final head.

The tests execute actual Rules, Application and engine-free Data/Dialogue sources
through `Tools/HeadlessTests/SocialStim.HeadlessTests.csproj` under .NET 8, with
warnings treated as errors. Unity's EditMode assembly includes the same test
source. Headless success does not compile or exercise Unity-specific adapters.

## Static checks

Run `python3 Tools/validate_scaffold.py` from the repository root. It resolves
its project root relative to the script, so running it from another working
directory is also supported. It has no third-party Python dependencies.

The validator checks:

- JSON and `.asmdef` syntax, registered project references, missing references,
  cycles, allowed layer dependencies, namespace roots and explicit engine access.
- Editor/test platform boundaries and known package assembly references.
- Every imported asset and folder has unique, correctly formed `.meta` metadata;
  orphan metadata is rejected and ignored `.gitkeep` files are not imported assets.
- Editor and direct dependency version pins match this milestone's environment.
- Unity Force Text, visible metadata, text line endings and trailing whitespace.
- Generated directories, IDE artifacts, and credential-like filenames are absent.

Also run `git diff --check`. Use `git check-ignore` to verify the exclusion of
`Library`, `Temp`, `Logs`, `UserSettings`, build output, IDE files, and `.env`.
The validator is tooling, separate from NUnit behavioral tests.

## Editor verification still required

Unity is unavailable in the creation environment. No editor import, Unity package
resolution, Unity-specific adapter compilation, Unity EditMode/PlayMode execution,
or player build was run. Headless C# compilation/tests are the distinct checks
reported above. No clean Unity import should be inferred from them.

1. Open in Unity 6000.3.26f1 and let Package Manager resolve the manifest.
2. Review generated settings and `Packages/packages-lock.json`; commit them on
   the feature branch, preserving the supplied metadata GUIDs.
3. Verify no Console errors; complete `Docs/UNITY_VERIFICATION.md`, including
   Input System activation, TMP resources, scene play, and both player factions.
4. Verify Force Text and Visible Meta Files, plus exact package versions.
5. Record the editor import and static-check results in the PR.

The game scene is prewired to the starter catalog, and the role/catalog assets
have committed metadata. Unity EditMode should discover the shared behavioral
suite. Milestone 01 reserved the PlayMode assembly; milestone 05 adds presenter
and TMP adapter checks. Full editor
validation remains an open gate before calling the scene verified.
