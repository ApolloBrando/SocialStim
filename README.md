# SocialStim

A single-player, text-based social deduction game built with Unity and C#.
Inspired by Town of Salem, Gnosia and Slaughter Horse 2.

## Current status

A work in progress for sure

Milestone 01 adds a complete participant-loop candidate: seven seeded characters,
five roles, discussion and deceptive claims, nomination, defense, judgement,
execution, night targeting, scoped results, and outcomes. All dialogue is marked
PLACEHOLDER. This independent milestone 05 branch adds presentation effects on
top of main; milestone 02 remains a separate PR, and 03/04 are deferred.

Day uses soft white text on near-black; night uses near-black on soft white.
The UI stays minimal so semantic emphasis and occasional effects stand out.
Typewriter, semantic accents, text shake, border/vignette, and a limited flash
are controlled by cue profiles. Reduced motion is enabled and flashes disabled
by default. The footer provides skip, effects, motion, flash, text and intensity
controls. Settings last for the current application session.
PLACEHOLDER. Milestone 02 adds attributed evidence, trust, constrained role/faction
estimates, public self-exposure, consistent bluffs, team planning, and conditional
dialogue. Save/resume, advanced roles, and effects remain later work.

Milestone 02 is being developed in parallel while the owner verifies milestone
01 in Unity. Editor verification is still pending.

## Run in Unity

1. Open the project in **Unity 6000.3.26f1** and let Package Manager resolve it.
2. Set **Project Settings > Player > Other Settings > Active Input Handling** to
   **Input System Package (New)**, then restart Unity if prompted. The package is
   already included; this disables the deprecated legacy Input Manager.
3. Import **Window > TextMeshPro > Import TMP Essential Resources**, or assign
   a TMP font asset to the scene's `GameBootstrap` component.
4. Run **Tools > SocialStim > Validate Starter Content**, then
   **Tools > SocialStim > Open Game Scene** and press Play.
5. Choose Random role or a role button. Scroll each panel independently; arrow
   keys/Enter navigate buttons, keep focused choices visible, and a `>` marker
   shows focus. Space or Skip reveals the pending batch; choices unlock afterward.

`Assets/_Project/Scenes/Game.unity` already references `StarterCatalog.asset`.
It builds the Canvas, transcript, choices and Input System EventSystem at startup.
No manual catalog/component wiring is needed. Missing input mode or font resources
are reported in the Console. Disable **Random Seed** on the scene's `SocialStim`
object and set **Fixed Seed** (for example `147`) for reproducible play. Trial
count, discussion rounds, and role reveal are also inspector options.
`StarterEffects.asset` is assigned to the scene for inspector-authored overrides.
Open **Tools > SocialStim > Effects Playground** to preview the portable timing
and palette without playing a game. See [Docs/EFFECTS.md](Docs/EFFECTS.md).

## Roles

| Role | Faction | Night action |
| --- | --- | --- |
| Inspector | Town | Inspect another participant's apparent alignment |
| Physician | Town | Protect another participant |
| Interrupter | Town | Block another participant's ability |
| Assassin | Antagonist | Select the single team kill, or pass |
| Survivor | Neutral | Spend one of two self-protection charges, or pass |

Everyone can accuse, defend, vouch, deflect, claim any role, nominate, and judge.
Agreement/disagreement and calls for trial also affect public social evidence.
From Day 2 everyone can report an alleged alignment, including lies. Choices
explain their social meaning without showing NPC belief scores. Your own role
and legitimate private results are visible; other living roles stay hidden.
Death enters spectator mode. New run returns to setup and discards run state.

## Social reasoning and authoring

The default `SocialNpcStrategy` uses each observer's evidence, credibility,
personality and visible pressure. `BasicNpcStrategy` remains injectable for
comparison. Neither reads other participants' secret roles or private beliefs.
Normalized estimates are heuristic beliefs, not calibrated probabilities, and
remain out of the normal text UI.

Tune `Assets/_Project/ScriptableObjects/Reasoning/DefaultReasoning.asset` and start
a new run to apply changes. The catalog already references it; no extra scene
wiring is required. Dialogue JSON version 2 supports declarative conditions and
contains 51 PLACEHOLDER lines. Version 1 content still loads. See
[Docs/SOCIAL_REASONING.md](Docs/SOCIAL_REASONING.md) for policies and checks.

## Verification

```sh
python3 Tools/validate_scaffold.py
dotnet test Tools/HeadlessTests/SocialStim.HeadlessTests.csproj --configuration Release
```

GitHub Actions compiles the actual Rules, Application, and engine-free dialogue
import code and runs shared NUnit behavioral tests, including 100 seeded runs
across Town and antagonist play. This intentional headless `.csproj` is the sole
exception to the exclusion of Unity-generated IDE project files.

**Unity import, adapter compilation, UI behavior, and player builds remain
unverified.** Follow [Docs/UNITY_VERIFICATION.md](Docs/UNITY_VERIFICATION.md) before
calling the scene verified. Milestone 05 has a separate
[effects verification gate](Docs/EFFECTS_VERIFICATION.md). Commit the
editor-generated package lock/settings on the feature branch after import. No runtime generative AI is used.

See [ARCHITECTURE.md](ARCHITECTURE.md), [Docs/GAME_DESIGN.md](Docs/GAME_DESIGN.md),
[Docs/DIALOGUE_AUTHORING.md](Docs/DIALOGUE_AUTHORING.md), and
[Docs/MILESTONES.md](Docs/MILESTONES.md). Direct publication to `main` requires
confirmation of the concrete batch. No license has been selected.

CI compiles the NUnit 3.5.0 API baseline and runs shared tests with NUnit 3.14.0. Collection
membership assertions use the older-compatible Has.No.Member API. This remains
separate from Unity compilation and editor execution.

Milestones 02 and 05 share a single speech event per line, carrying public
intent/target metadata after observer evidence is updated. Nomination ballots
are chosen before publication and enter public evidence; deaths also enter
evidence when role reveal is disabled. Integration regression tests cover all
three contracts.
