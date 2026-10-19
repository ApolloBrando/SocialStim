# Save/resume contract and verification

## User flow

Save and Load appear in the top toolbar. They are unavailable while dialogue,
effects, or reading pauses are pending. Save writes a single local run slot.
Load validates the primary; if invalid or missing, it attempts the previous
backup. Only a fully validated new session replaces the current presenter.
Errors leave the active run unchanged. New run creates fresh gameplay state
and keeps the explicit saved slot available until the next Save.

Accessibility controls and text speed persist independently. Settings errors
use defaults without changing gameplay state. Resumed transcript history is
shown in full; it is not re-simulated. The same input revision/choices return.
Rendering clocks, effects and transient menu category/page are reset.

Files are under Application.persistentDataPath/SocialStim: run.sstim,
settings.sstim, and optional .bak previous versions. These are private local
state, not a public or spoiler-free export.

## Format version 1

The envelope contains a magic number, integer schema version, content hash,
SHA-256 payload checksum, payload length, and explicit binary payload. Limits
bound file size, string bytes, counts, IDs, enums, ranges, and stable input
phases. No arbitrary serialized classes, callbacks, Unity objects, reflection
restore, or executable type names are accepted.

The payload stores run options, supported strategy/policy identity and tuning,
original full cast and resources/alive state, phase/day/revision/trial/discussion
counters, pending defendant and outcome, opening state, decision RNG state,
last team plan, execution objectives, recorded last actions, role reveals, all
recipient-scoped night results, every observer's evidence/report assertions,
scores/claims/distribution weights and known facts, dialogue RNG/history, and
the player-scoped transcript including public semantic fields and metadata.

Initial faction totals are reconstructed from the full cast, including dead
participants; they are never recalculated from surviving hidden roles.
Private results remain attached only to their legitimate recipient. The save
itself contains simulation secrets, and is not passed through a player view.

The compatibility fingerprint includes the rules revision, full catalog role
data, authored dialogue text/conditions/metadata, and belief tuning. Changed or
missing content is rejected. There is no migration from future/unknown schema
versions. Checksums are corruption detection, not a security signature; saves
are not encrypted. Maximum payload is 8 MiB.

Production SocialNpcStrategy/BasicNpcStrategy and StarterWinCondition/
CapabilityWinCondition are supported explicitly. Custom injected strategies
or policies throw a clear unsupported-save error rather than silently dropping
private implementation state.

## Atomic writes and recovery

A same-directory temporary file is fully written and flushed. File.Replace
atomically installs it and retains the previous file as backup; initial saves
use an atomic move. If replacement is unsupported or fails, the old primary is
preserved and an error is shown. There is no delete-before-write fallback.
Temporary files are cleaned on ordinary failure. A process crash can leave a
temporary file; it is not considered a valid slot automatically.

RunSaveService validates the primary before updating backup history. If it is
already invalid, an existing backup is preserved while a new primary is
installed. Recovery reads and validates a backup without rewriting either
file. Saves/settings use fixed slot names, not UI-supplied paths.

## Automated coverage

Round trips compare complete snapshot bytes at input boundaries across several
roles/seeds, then submit identical commands and compare complete continuation
again. This exercises stream state, resources, public/private observations,
beliefs and dialogue selection. Pending choice IDs/labels also remain identical.
Separate trial tests capture Defense, Judgement, LastWords and terminal state.
Corruption, truncation, unknown versions, changed/missing content, reasoning
mismatch and unsupported custom strategies are rejected. File-store tests cover
backup recovery/preservation and failed writes. Settings round-trip separately.

## Unity gate still pending

Import and compile in Unity 6000.3.26f1, then verify Save/Load across an
investigation, exhausted charges, trial, death/spectating and terminal outcome.
Restart Play mode and verify settings and text speed. Verify that a failed load
does not reset the running game. Check backup recovery using a copied test save.
Verify real filesystem atomic replacement on supported player platforms and
actual adapter/UI compilation. Windows player builds remain deferred by the owner.
