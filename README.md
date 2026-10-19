# SocialStim

A single-player, text-based social deduction game built with Unity and C#.
Inspired by Town of Salem, Gnosia and Slaughter Horse 2.

## Current status

Milestones 01, 02 and 05 are merged. This branch adds milestone 03: eleven
role definitions, an authored nine-participant setup with repeatable Citizens,
advanced night interactions and outcomes, morning reporting, grouped choices,
and the owner's playtest UI changes. Runs are intended to be completed in one sitting;
ongoing-run save/resume has been dropped.

Dialogue is authored data marked PLACEHOLDER; no runtime generative AI is used.
The player belongs to the cast and follows the same command legality as NPCs.

Day uses a light background that warms from pale yellow toward soft red through
trial phases; night uses a dark background with light text. Framed participant
cards, grouped choices, labelled role claims and an unverified claim board keep
the interface readable. Text reveals at 28 graphemes/second by default with
stable speaker speeds and varied reading pauses. Choices wait for the player's
turn. Skip, instant text, reduced motion, flashes, and intensity are available.
Text speed and accessibility preferences persist between sessions; run state does not.

New shared compilation and behavioral checks are pending in CI. Unity import,
adapter compilation, actual rendering/input, and player builds remain a
separate verification gate; the current changes are review candidates.

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
| Citizen | Town | No night ability; repeatable good role |
| Vigilante | Town | Independent basic attack with two charges |
| Forger | Antagonist | Frame another's apparent alignment for one night |
| Coordinator | Antagonist | Observe actual visitors to another participant |
| Provocateur | Neutral | No night ability; seek execution as a personal objective |
| Lone Killer | Neutral | Independent powerful attack; basic innate defense |

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
contains 54 PLACEHOLDER lines. Version 1 content still loads. See
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

## Playtest flow and milestone 03

The authored starter list now has nine participants, a guaranteed repeatable
Citizen slot, six Town slots, two Antagonist slots, and one Neutral slot.
The catalog adds Citizen, Vigilante, Forger, Coordinator, Provocateur, and Lone
Killer. Roles are definitions, not mandatory unique characters. See
[role interaction policies](Docs/ROLE_INTERACTIONS.md).

The player may open discussion or yield to one random NPC. Existing role
claimants report their findings when discussion begins after dawn. The player
can report the previous night's recorded selection and legitimate results.
Reports are public assertions, and false claims remain possible.

Choices use categories, action families, and pages. Browsing and Back do not
spend a turn. Choices stay hidden during dialogue playback and its reading
pause. Default text speed is 28 graphemes/second with stable speaker tempo and
variable 0.65–1.65 second pauses. Skip and instant text bypass the wait.
Role claims have a label, semantic highlight, and persistent unverified claim
board. uGUI panel/button frames are visible independently of animated effects.

Day has a light background, warming from pale yellow toward soft red as trial
phases advance; night has a dark background with light text. This follows the
latest playtest correction and supersedes the original inverse palette.

Compilation, behavioral checks, and Unity rendering must be reported separately.
The workspace runner was unavailable during this revision; CI is the compile
and shared-test gate. Unity adapter/rendering verification is still pending.

Participants also have individual framed uGUI cards. Public status and cast
updates wait for pending dialogue to finish, so upcoming deaths or reveals
are not exposed ahead of their lines. The editor content validator checks that
every selectable role fits the authored setup.

## Cozy UI and one-sitting runs

Ongoing-run save/resume has been cancelled. Draft PR #6 is closed; this branch
contains no snapshot files, run slots, save/load toolbar, or continuation APIs.
New run always starts fresh. Only UI preferences (speed, instant text, effects,
reduced motion, flash and intensity) persist through Unity PlayerPrefs.

The native uGUI interface now uses rounded paper-like panels, quiet warm frames,
subtle shadows, a slate night palette, a sage focus tint, a clear title, compact
participant cards, and smaller action descriptions. Daylight warmth and role
claim emphasis remain. Surfaces are cached native UI geometry, with no added
art/font/package download. Full HD rendering still requires Unity verification.

Role setup is grouped by faction, and the footer keeps advanced display
controls behind Display options. Skip, speed and keyboard focus remain visible.
