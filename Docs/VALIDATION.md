# Milestone 00 validation

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
The validator is tooling, not a suite of Unity gameplay tests or a security audit.

## Editor verification still required

Unity is unavailable in the creation environment. No editor import, package
resolution, C# compilation, EditMode tests, PlayMode tests, or player build was
run. No test success or clean import should be inferred from static checks.

1. Open in Unity 6000.3.26f1 and let Package Manager resolve the manifest.
2. Review generated settings and `Packages/packages-lock.json`; commit them on
   the feature branch, preserving the supplied metadata GUIDs.
3. Verify no Console errors. Warnings about assemblies with no scripts are
   expected until milestone 01, not justification for dummy gameplay classes.
4. Verify Force Text and Visible Meta Files, plus exact package versions.
5. Record the editor import and static-check results in the PR.

There are no scene assets or inspector connections in this milestone, and no
test methods are expected to be discovered. Full editor validation remains an
open gate before calling this a verified Unity project.
