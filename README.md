# SocialStim

A single-player, text-based social deduction game built with Unity and C#.
Inspired by Town of Salem, Gnosia and Slaughter Horse 2.

## Current status

A work in progress for sure

Milestone 01 adds a complete participant-loop candidate: seven seeded characters,
five roles, discussion and deceptive claims, nomination, defense, judgement,
execution, night targeting, scoped results, and outcomes. All dialogue is marked
PLACEHOLDER. Save/resume, advanced reasoning, and visual effects are later work.

## Run in Unity

1. Open the project in **Unity 6000.3.26f1** and let Package Manager resolve it.
2. Set **Project Settings > Player > Other Settings > Active Input Handling** to
   **Input System Package (New)**, then restart Unity if prompted. The package is
   already included; this setting disables the deprecated legacy Input Manager.
3. Import **Window > TextMeshPro > Import TMP Essential Resources**, or assign
   a TMP font asset to the scene's `GameBootstrap` component.
4. Run **Tools > SocialStim > Validate Starter Content**, then
   **Tools > SocialStim > Open Game Scene** and press Play.
5. Choose Random role or a role button. Scroll each panel independently; arrow
   keys/Enter navigate buttons and a `>` marker shows keyboard focus.

`Assets/_Project/Scenes/Game.unity` already references `StarterCatalog.asset`.
It builds the Canvas, transcript, choices and Input System EventSystem at startup.
No manual catalog/component wiring is needed. Missing input mode or font resources
are reported in the Console. Disable **Random Seed** on the scene's `SocialStim`
object and set **Fixed Seed** (for example `147`) for reproducible play. Trial
count, discussion rounds, and role reveal are also inspector options.

## Roles

| Role | Faction | Night action |
| --- | --- | --- |
| Inspector | Town | Inspect another participant's apparent alignment |
| Physician | Town | Protect another participant |
| Interrupter | Town | Block another participant's ability |
| Assassin | Antagonist | Select the single team kill, or pass |
| Survivor | Neutral | Spend one of two self-protection charges, or pass |

Everyone can accuse, defend, vouch, deflect, claim any role, nominate, and judge.
From Day 2 everyone can report an alleged alignment, including lies. Choices
explain their social meaning without showing NPC belief scores. Your own role
and legitimate private results are visible; other living roles stay hidden.
Death enters spectator mode. New run returns to setup and discards run state.

## Verification

```sh
python3 Tools/validate_scaffold.py
dotnet test Tools/HeadlessTests/SocialStim.HeadlessTests.csproj --configuration Release
```

GitHub Actions compiles the actual Rules, Application, and engine-free dialogue
import code and runs shared NUnit behavioral tests, including 100 seeded runs
across Town and antagonist play. This intentional headless `.csproj` is the sole
exception to the exclusion of Unity-generated IDE project files.

CI compiles against NUnit 3.5.0 and runs the suite against 3.14.0. The older API check
catches assertion overloads missing from Unity's NUnit integration; it does not
replace compiling the actual Unity test assemblies in the editor.

**Unity import, adapter compilation, UI behavior, and player builds remain
unverified.** Follow [Docs/UNITY_VERIFICATION.md](Docs/UNITY_VERIFICATION.md) before
calling the scene verified. Commit the editor-generated package lock/settings on
the feature branch after import. No runtime generative AI is used.

See [ARCHITECTURE.md](ARCHITECTURE.md), [Docs/GAME_DESIGN.md](Docs/GAME_DESIGN.md),
[Docs/DIALOGUE_AUTHORING.md](Docs/DIALOGUE_AUTHORING.md), and
[Docs/MILESTONES.md](Docs/MILESTONES.md). Direct publication to `main` requires
confirmation of the concrete batch. No license has been selected.
