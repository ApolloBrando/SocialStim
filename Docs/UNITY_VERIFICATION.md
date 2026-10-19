# Unity verification gate: milestone 01

Status: requires a machine with Unity 6000.3.26f1. The creation environment has
no Unity editor or license. Headless C# compilation/tests pass independently;
they cannot establish scene import, TMP rendering, Input System behavior, or a
working player build. Do not mark these boxes complete without running them.

## Import and configuration

- [ ] Check out `milestone/01-core-loop` in `ApolloBrando/SocialStim`.
- [ ] Open the repository root using Unity **6000.3.26f1**.
- [ ] Resolve packages, inspect Console errors, and preserve all committed GUIDs.
- [ ] Set Player Settings > Other Settings > Active Input Handling to **Input
  System Package (New)** or **Both**; allow the editor restart if prompted.
- [ ] Import Window > TextMeshPro > **TMP Essential Resources**, or assign an
  existing TMP font on `GameBootstrap`. Commit imported assets with metadata and
  review their license files. This delivery contains no third-party font bytes.
- [ ] Confirm Force Text serialization and Visible Meta Files.
- [ ] Run Tools > SocialStim > **Validate Starter Content**; expect 5 roles,
  45 placeholder lines, and 20 names.
- [ ] Review and commit the generated `Packages/packages-lock.json` and missing
  default ProjectSettings files on this feature branch, excluding user/cache files.

## Plain-text scene

- [ ] Open Tools > SocialStim > **Open Game Scene** and press Play.
- [ ] Verify setup and all role choices render, with no Console errors.
- [ ] Verify both panels scroll at a typical desktop resolution and long choices
  remain readable. Arrow keys/Enter select buttons; the `>` marker shows focus.
- [ ] Confirm the scene root references `StarterCatalog.asset` and no manual
  GameObject wiring is needed beyond the optional font override.
- [ ] Start as Inspector. Public roles stay hidden; own role is visible. From
  Day 2 private investigations appear only in the player's transcript.
- [ ] Start as Assassin. Choose/pass kills; make false role claims and fabricated
  reports. Confirm that choices affect public discussion without revealing truth.
- [ ] Start as Survivor. Self-protection uses one charge when executed; passing
  and being blocked preserve it.
- [ ] Verify nomination, defendant defense, public judgement, last words,
  execution/reveal, repeated days/nights, and final outcome screens.
- [ ] Verify a dead player has only spectator continuation; no dead votes/actions.
- [ ] Disable Random Seed, set seed `147`, use the same role and inputs twice,
  and compare names, public events, choices, private results, and outcome.
- [ ] New run returns to setup and discards transcript/beliefs/resources.

## Editor tests and build

- [ ] Run EditMode tests in Unity Test Runner. These share behavioral source with
  the headless suite, while compiling against Unity's runtime/framework assemblies.
- [ ] Build the enabled `Game.unity` scene for Windows desktop with the current
  Mono backend, run it, and repeat setup/input checks. IL2CPP is not validated.
- [ ] Record actual results, editor/version, Console errors and reproduction steps
  in the PR. Copy the first error text and affected file/line if compilation fails.

Passing this gate permits milestone 02 to proceed on a verified Unity foundation.
Merge or any direct publication to `main` still requires owner authorization.
