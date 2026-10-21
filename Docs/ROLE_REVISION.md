# Role names, guilt, execution and claim evidence

Baseline: main ba67090. This branch follows the requested role changes. IDs, asset paths, metadata GUIDs, serialized field names and outcome enum values remain stable. The display names are Villager, Sheriff, Vigilante, Lookout, Forger, Mafioso, Strongarm, Serial Killer, Jester and Ego; Physician retains its name. The internal Antagonist faction is still the Mafia faction. This does not change the existing priority pipeline or general parity policy.

## Rules and decisions

- The authored Vigilante now schedules guilt only when its executed attack actually kills a true Town member. A blocked, protected, immune or failed shot does not qualify; forged appearance does not change true alignment. Guilt resolves at the end of the *following* night, prevents another ability selection, and cannot be blocked or protected. A simultaneous attack does not cancel an accepted shot. Existing victory evaluation can end the game before another night occurs.
- An executed Jester earns its existing personal objective. Before night starts, it may kill one living Town participant, regardless of their ballot. Mafia, Neutrals and dead participants are excluded. The former guilty-only restriction could silently cancel an NPC Jester reward when Town voted innocent; it has been removed to match the requested objective. No eligible target means no reward kill. Night deaths never grant revenge.
- An executed player Jester gets a validated, revision-scoped target choice before spectator advancement resumes. NPC Jesters choose from the granted eligible identities using their own suspicion/trust, with seat order breaking equal scores. Revenge precedes terminal evaluation, so a pending reward cannot be lost to early parity or the day cap. It bypasses night protection because night has not begun.
- Each participant can publish only one role claim per run, whether it starts as a normal claim or a counterclaim. An existing claim prevents later counterclaim prompts, commands and NPC emissions; claiming one role cannot be followed by counterclaiming another. Passing a counterclaim opportunity does not consume the allowance. The first counterclaim remains available to someone who has not claimed yet.
- When a claimant of a unique role dies at night, surviving claimants attract modest suspicion. A public reveal confirming the deceased claim strengthens it; a reveal exposing the deceased as a liar prevents it. Both deaths are processed before inference, so dead rivals cannot attract new pressure. Evidence is deduplicated. The inference never establishes hidden truth or changes setup counts.
- Executions sometimes receive one seeded reaction from a living NPC, before night. Public reveal reactions use existing trait-specific reveal lines; hidden-role executions use death reactions. No reaction is guaranteed and the executed participant cannot speak again.
- Fake claimants already give structured fabricated nightly reports. The existing morning publication and once-per-night checks remain; reports contain the asserted role rather than privileged true-role flags. Existing all-role report regressions continue to cover this behavior.

Every role reference now uses the same Ability / Limits / Claims / Goal structure, with current mechanics rather than future mission promises. Displayed new copy is editable in GameText.json, with matching validated portable defaults.

## Verification

The existing all-role matrix covers passing, self/unknown/exhausted targets, blocking, simultaneous death, protection, investigation appearance, night reports and execution for all eleven authored roles. New tests cover successful guilt across every other role, blocked/protected shots, unavoidable delayed guilt, player/NPC Jester choices, ballot-independent Town eligibility, no-target cases, duplicate submissions, hidden/revealed claimant deaths, one counterclaim per player and optional living-NPC execution reactions.

The full portable suite passed 366 tests before the final two regressions were added. All 24 new role revision cases then passed. NUnit 3.5/net472 compatibility compilation passed with zero warnings/errors. Python scaffold validation and all four validator regression tests passed. No Unity compilation, native EditMode/PlayMode execution, player build or balance claim is implied.

## Manual Unity checks

1. Import in Unity 6000.3.26f1. Check the renamed roles and wrapped references; every role should have the same description headings.
2. Shoot a Town member as Vigilante: survive the shooting night, lose further night targets, then die at the end of the following night. Repeat with blocked/protected shots and non-Town targets.
3. Execute a player Jester. Choose one living Town participant directly from the roster. Check that innocent/abstaining Town voters can be chosen, and non-Town participants cannot and night does not begin before the choice. Repeat with an NPC Jester and no eligible Town voter.
4. Counterclaim once, then encounter another role claim. Check that no second counterclaim is offered. Verify NPCs obey the same limit.
5. Observe occasional execution reactions and claimant-death pressure with reveal on/off. Check that neither discloses unrevealed roles to living participants.

No files were deleted. No README edits, dependencies or GUID changes.
