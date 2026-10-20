# Familiar role names and final actions

## Naming and content

Only display names change; internal role IDs, asset paths, GUIDs and historical outcome enum values remain stable.

| Previous | Current | Stable ID |
| --- | --- | --- |
| Solarian | Villager | citizen |
| Decryptor | Sheriff | inspector |
| Nullifier | Vigilante | vigilante |
| Watcher | Lookout | coordinator |
| Aperture | Forger | forger |
| Archon | Mafioso | assassin |
| Singer | Strongarm | interrupter |
| Anarchy | Serial Killer | lone-killer |
| Fool | Jester | provocateur |
| Infinitum | Ego | survivor |

Physician retains its name. All eleven role-reference descriptions use the same five fields: Alignment, Action, Limits, Goal and Claim. The role name already appears as the card heading. Descriptions, new result messages and final-act wording live in editable GameText.json with validated compatibility defaults. Win messages use Serial Killer and Ego. The separate QoL PR supplies explicit role-reference wrapping.

## Vigilante: original Town of Salem style

The requested delayed-guilt rule matches the original Town of Salem behavior rather than the newer Town of Salem 2 gun-put-away rework. [Official original role descriptions](https://www.townofsalem.io/roles/) describe three shots and guilt for shooting Town; [the developer's ToS2 rework notes](https://www.townofsalem.io/town-of-salem-2-season-4-patch-notes-r1-4-00/) distinguish that later variant. This implementation uses three basic shots, cannot shoot on Night 1, and schedules guilt death for the night after actually killing a Town participant.

Passing or being blocked preserves a shot. Protection or innate defense that prevents the kill does not cause guilt; an executed failed shot still uses a bullet. A true Town victim causes guilt even if Forger changed their apparent alignment. Antagonist or Neutral targets do not cause guilt. Killing Town removes all remaining shots, including after the last bullet, and privately warns the Vigilante. Guilt resolves after the next night's ordinary actions and cannot be prevented by protection, defense or role blocking. Already-terminal outcomes still end the match normally.

The generic role fields FirstActionNight and GuiltOnTownKill are detached portable data with serialized authoring counterparts. NightActor carries a pending guilt night; the ordered GuiltStage handles unavoidable scheduled deaths. Only the shipped Vigilante enables these rules. The session routes the recorded guilt result through the existing scoped report path.

## Jester final act

A day execution completes the Jester's objective and grants one final choice of a living Town participant to kill, before night begins. This Town-only choice is the owner's requested rule, not a claim to copy Town of Salem's exact voter-based revenge mechanic.

A player Jester receives a dedicated input request even though they are dead. Non-Town, dead and self targets are rejected. An NPC Jester receives the same ability-authorized candidate list through its scoped strategy view. If the strategy supplies an invalid target, the session picks a legal one with the seeded decision stream. If no Town participants remain, the choice is skipped. A night death grants no final act. Revenge is not a normal night attack and cannot be prevented by that night's protection or block.

The extra death happens before terminal evaluation because it may change the faction result. Revenge cannot be repeated. GamePhase.JesterRevenge and CommandKind.ChooseRevengeTarget are appended to preserve existing enum values. The shipped Jester enables RevengeOnExecution; legacy synthetic fixtures without that capability keep their previous behavior. Revenge is not misclassified as a night death for claim reasoning.

## More useful public reasoning

When a night victim claimed a unique role that another living participant also claims, each observer records the competing-claimant loss and increases suspicion of the survivor. A public reveal confirming the dead claimant strengthens this response; a reveal showing the dead claimant was bluffing suppresses that inference. Hidden deaths remain uncertainty, not proof. A day execution does not trigger the night-only signal. The evidence is deduplicated and contributes to case strength without converting a guess into hard faction knowledge or giving an observer access to another actor's real role.

Fake NPC claimants already publish a cover-role report each morning. Six new cases cover every shipped bluffing role: Mafioso, Strongarm, Forger, Serial Killer, Ego and Jester. Reports use the asserted Sheriff role in the fixture and enter observers' attributed evidence. A passive liar can report passing; the system does not invent a secret real night order for them. A liar with an active order can reuse their own target in a false role account without reading other actors' hidden actions.

## Checks and native follow-up

The combined role/QoL tree passes all 367 portable C# tests. Python scaffold checks and all four validator tests pass; NUnit 3.5 compatibility compilation has zero warnings/errors. The role-only tree passed 355 tests before integrating the separately published QoL work.

New regressions cover names/description structure, first-night gating, all victim factions, blocked/protected shots, last-bullet guilt, forged Town victims, delayed unavoidable death through the real session, six fake-report producers, competing claimants and player/NPC Jester revenge including terminal ordering. Existing all-role edge tests are updated for the intentionally changed Vigilante and Jester contracts.

Unity is unavailable: portable tests and syntax parsing do not verify Unity APIs or a player build. Native follow-up: role selection/reference/claims use the requested names; Vigilante Night 1 has no targets and Night 2 does; guilt warnings/reports and next-night death are clear; dead-player Jester choice remains interactive rather than auto-spectating; NPC revenge precedes Night; direct roster interaction works after the separate QoL PR is combined.
