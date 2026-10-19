# Contributing

## Workflow

Work only in `ApolloBrando/SocialStim`. One milestone normally gets one feature
branch and PR. The owner has permitted direct `main` publication for this test
project, with confirmation before the concrete batch is written. Prepare and
validate that batch before requesting confirmation. Do not merge without
authorization, force-push, rewrite history, or delete existing assets without
approval. Inspect current contents before each milestone and preserve existing
structure unless a reviewed change requires otherwise.

Use small Conventional Commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`,
or `chore:`. Keep unrelated changes separate. Every milestone updates README and
ARCHITECTURE and includes reproducible verification steps plus remaining issues.

An empty repository needs an initial commit. After the owner's confirmation,
initialize the default branch with README through GitHub's contents API, then
publish the rest of the prepared scaffold using that commit as the parent.
The earlier fallback bundle contains a local empty root on `bootstrap/empty`
and project changes on `milestone/00-project-foundation`; those local commit
hashes are not remote publication receipts. Never claim publication until the
resulting GitHub branch and tree have been verified.

## C# conventions

- One top-level type per file, filename matching its type, namespace matching
  the folder below the assembly namespace root.
- PascalCase types, properties, methods, and events; I-prefixed interfaces;
  `_camelCase` private fields; descriptive identifiers without abbreviations.
- `[SerializeField] private` fields for Unity authoring; expose behavior and
  read-only views rather than public mutable state.
- XML documentation on public APIs; useful short comments only.
- Constructor injection in plain C#; one composition root for Unity adapters.
- Composition over inheritance; shallow inheritance only for real is-a relations.
- No unused usings, dead scripts, static service locators, or gameplay calls to
  `UnityEngine.Random`. Do not depend on dictionary enumeration for seeded output.

Use `.editorconfig`. Test behavior and invariants, not internal method layouts.
Do not add fake placeholder types to silence empty-assembly warnings.

## Unity hygiene

Keep assets under `Assets/_Project`. Commit assets and their `.meta` files
together. Move assets inside Unity or move the existing metadata with them.
Do not regenerate GUIDs for existing assets. Commit Force Text and Visible Meta
Files settings. Keep editor tooling and tests behind their assembly/platform
boundaries. Rules and Application have no UnityEngine references.

Commit package manifest and editor-generated package lock together when package
resolution is available. The initial manifest pins direct versions; its lock is
pending the first verified editor import. Review auto-generated settings in that
same feature branch. Do not commit machine paths, secrets, signing keys, IDE
files, generated project files, caches, build outputs, or personal settings.

`.gitattributes` normalizes source/YAML line endings and marks binary formats.
No LFS patterns are enabled because this milestone has no large binary assets.
When large art/audio/source assets arrive, add narrowly scoped LFS rules and
document Git LFS installation before committing them. Never put Unity YAML,
dialogue JSON, or metadata behind LFS by default. Existing history migration
requires explicit authorization; do not rewrite history to introduce LFS.

## Pull requests

Use `.github/pull_request_template.md`. State resulting behavior, scope, setup,
checks actually run, checks unavailable, and open decisions. Never report Unity
tests as passing when only static validation ran. A PR stays unmerged until the
owner authorizes merge.

## Content and accessibility

Final dialogue belongs to the project owner. Any engineering example text must
be clearly marked `PLACEHOLDER`. Do not introduce runtime generative AI services.
Effects are a later milestone. They must support skipping, reduced motion,
adjustable intensity, limited/disabled flashes, and non-color-only information.
