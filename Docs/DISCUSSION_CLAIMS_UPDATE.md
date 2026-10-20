# Discussion, claims, verdict and roster revision

The owner's latest request supersedes the mission's direct plurality elimination
and continuous shaking. This revision keeps seeded, portable rules and uses the
existing dialogue schema, scene, catalog and uGUI components.

## Implemented behavior

- Default population is seven, including the player. Both the scene's serialized
  value and GameBootstrap default are seven. The Inspector count remains modular.
  Scaled setups allow two antagonists at seven so selecting a support antagonist
  does not make the single mandatory team-kill slot impossible.
- All authored acting roles are unique, including the player. Solarian may repeat.
  Infinitum and Fool are also unique passive roles.
- CanClaimTruthfully and CanBluff are authored role flags. Only Town acting roles
  may be claimed. Town may truthfully claim its own special role; Antagonist and
  Neutral roles may impersonate a Town special. Neither personality nor pressure
  permits a Town bluff. Solarian and hostile roles cannot themselves be claimed.
- Proactive bluffs select an unclaimed Town special. Duplicate-role challenges
  happen in an immediate CounterClaim window, with separate dialogue. The genuine
  NPC role holder counters; at most one NPC bluffer joins. Antagonists do not
  challenge a teammate's claim of that role. The player can counter or wait.
  The subsequent AI refinement makes every player counterclaim or pass consume a
  discussion round, like every other accepted player action. See AI_REFINEMENT.md.
- Town power roles keep cover unless exposed or holding important hostile evidence;
  a threatened NPC can choose a legal claim instead of its automatic defense.
- The ordinary menu is Wait, Accuse, Defend, and a conditional Claim Role group.
  Accuse/Defend open living NPC targets; Back navigates without spending a turn.
  Defending another character reuses the existing Vouch intent and authored lines.
  Refute turns pressure onto the accuser using the existing Deflect command.
- A seeded subset of NPCs speaks in each exchange. Optional Agree/Disagree
  responses, based on each listener's permitted beliefs, can precede an addressed
  player's response and follow a defense. The player may join at the next floor
  window. A response never forces a prompt after every ordinary NPC sentence.
- Wait/StaySilent advances the turn without selecting dialogue or publishing speech.
- Plurality chooses a defendant, not a death. The defendant gives a final defense,
  then every other living participant chooses Innocent/Guilty/Abstain. Guilty must
  exceed Innocent; abstentions do not count toward either. Acquittal ends that day's
  default vote. Guilt leads to optional last words and execution. Existing optional
  Trial Mode keeps its multiple-trial policy.
- A tied nomination gets one restricted revote between the tied candidates. A
  second tie, or everyone abstaining, ends the vote without a defendant. The
  existing configurable tie/abstention switches remain available.
- Spectating progresses automatically after queued text finishes, with a short
  pause between state advances. No Watch button is shown. Ended runs stop.
- One vertical player panel replaces the horizontal player strip and claims row.
  It shows each name, life state and three-letter abbreviation of the public
  claimed role. Badges use the claimed role's color; dead names/badges are grey
  and explicitly marked DEAD. No unrevealed role is exposed.
- Default shake is timed to 1.5 seconds. The TMP adapter also caps fear-tag motion
  and custom shake tracks at 1.5 seconds per displayed line, restores baseline
  vertices at expiry and continues honoring reduced motion.

## Role names and preserved identities

Asset paths, stable role IDs and GUIDs are retained so serialized references survive.

| Existing asset / ID | Current display name | Change beyond name |
| --- | --- | --- |
| Inspector / inspector | Decryptor | Unique; truthful Town special |
| Physician / physician | Physician | Unique; truthful Town special |
| Citizen / citizen | Solarian | Non-claimable, repeatable basic Town |
| Vigilante / vigilante | Nullifier | Unique; truthful Town special |
| Assassin / assassin | Archon | Unique; Town-special bluff permission |
| Interrupter / interrupter | Singer | Antagonist support blocker, not Town |
| Forger / forger | Aperture | Unique; Town-special bluff permission |
| LoneKiller / lone-killer | Anarchy | Unique; Town-special bluff permission |
| Survivor / survivor | Infinitum | Passive night immunity; replaces a normal antagonist victory while alive |
| Coordinator / coordinator | Watcher | Town investigative visitor watcher |
| Provocateur / provocateur | Fool | Existing vote-execution personal objective |

Infinitum reads Town/CLEAR through the existing alignment system. It cannot win
just by surviving a Town victory. Anarchy plus Infinitum alone is a draw: Anarchy
cannot kill the night-immune survivor, and a two-person restricted tie cannot
eliminate either by voting. Infinitum waits for the configured normal antagonist
win condition; this revision retains the existing capability-based resistance
policy. The wider mission's three-way Decryptor readouts, Nullifier backlash,
Archon promotion, and full healing rework are still separate outstanding work.
Renamed assets do not establish completion of those mechanics.

## Dialogue inspection and editing

Edit `Assets/_Project/Data/Dialogue/AnxiousDialogue.json`, `BraveDialogue.json`,
`SlowDialogue.json`, `ChatterboxDialogue.json`, `SmugDialogue.json`,
`GrimDialogue.json` and `JokesterDialogue.json`. These are separate existing-schema
files, one per voice. `StarterDialogue.json` holds the neutral player templates.

This revision adds eight CounterClaim lines per NPC trait (56 total) and four
player counterclaims, retaining every original dialogue ID. Current totals are
898 NPC lines plus 72 player templates = 970. Brave has 130; every other NPC trait
has 128. The generated DIALOGUE_COVERAGE.md reports each voice/action separately.

Previously, recent-line memory held only three selections and specificity could
repeatedly pin a single conditional line. Selection now exhausts eligible unused
lines per speaker/action before repeating, broadens inside the same voice when
a specific tier has been used, and prefers the least-used lines across the run.
Existing hard role/evidence gates are never relaxed. Silence records remain in
the files for compatibility but are no longer spoken.

## Name replacements and removals

All twenty conventional names remain. Thirty-eight multiword entries are replaced
in the same existing pool; the name-selection algorithm is unchanged. The sixty current
names are unique, single-word ASCII names of at most sixteen letters.

| Removed pool entry | Replacement |
| --- | --- |
| Aster Venn | Aster |
| Bex Marrow | Bex |
| Cyr Olan | Cyr |
| Dax Meridian | Dax |
| Edda Rook | Edda |
| Fen Vesper | Fen |
| Galen Vox | Galen |
| Hessa Tarn | Hessa |
| Ivo Kestrel | Ivo |
| Juno Vale | Juno |
| Kes Ardent | Kes |
| Luma Sorn | Luma |
| Miro Thane | Miro |
| Nessa Vey | Nessa |
| Olin Strake | Olin |
| Perrin Lux | Perrin |
| Rhea Solace | Rhea |
| Sable Tern | Sable |
| Ulan Crest | Ulan |
| Zev Orison | Zev |
| Admiral Waffle | Waffle |
| Bean Counter | Beans |
| Commodore Socks | Socks |
| Duke Noodle | Noodle |
| Eggbert | Eggbert (retained) |
| Fizz Bucket | Fizz |
| Gravy Jones | Gravy |
| Hat Inspector | Hat |
| Ink Biscuit | Biscuit |
| Jelly Admiral | Jelly |
| Kettle Pop | Kettle |
| Lunchbox | Lunchbox (retained) |
| Mop Regent | Mop |
| Nugget Supreme | Nugget |
| Oat Baron | Oat |
| Plum Socket | Plum |
| Queen Turnip | Turnip |
| Rivet Muffin | Rivet |
| Sir Breadcrumb | Breadcrumb |
| Toast Marshal | Toast |

Thirty-eight multiword names are removed/replaced; Eggbert and Lunchbox were already
single words. No source file, role ID, asset GUID or dialogue row is deleted. The
generated Role Claims Board, its summary/cache, the horizontal roster layout and
the old category/family menu tier are removed/replaced in the view. Watch remains
an internal portable continuation option for headless clients, never a rendered
spectator button. Updated tests replace obsolete expectations that silence speaks,
Town can bluff, every NPC must speak, or plurality executes immediately.

## Portable validation

The completed shared suite passes **186 tests, zero failures/skips**, including
66 seven-player games using the actual authored role definitions, all-role setup
selection across twenty seeds, immediate-counterclaim regressions, voice coverage
and the existing seeded full-catalogue runs. The NUnit 3.5.0 compatibility build
passes with zero warnings/errors. Scaffold validation, four Python validator tests
and git diff --check pass. Two additional Unity-facing menu/TMP tests are authored
but not included in the portable count and have not been run.

## Native Unity checklist (not run here)

1. Open Unity 6000.3.26f1, compile, resolve packages and run Validate Starter Content.
   Run EditMode and PlayMode tests, including the new real-menu and timed-TMP checks.
2. Start a run and confirm seven participants, single-word names and no duplicate
   acting roles. Choose each special and verify setup succeeds; also change the
   Inspector population count to verify that it is configurable.
3. As Solarian, verify Wait/Accuse/Defend and no claim group. Check every target,
   Back, hover/focus, arrows/Enter and mouse. As a Town special, claim only yourself.
   As Antagonist/Neutral, verify only Town acting roles can be impersonated.
4. Claim an occupied special role: the true NPC holder must counter immediately.
   Wait for an NPC claim and counter as the player. Check all trait counterclaim
   voices and that multiple antagonists do not pile onto their teammate's bluff.
5. Read discussion batches before/after player windows. Verify Agree/Disagree,
   defense support and Refute target the right public statement; Wait speaks nothing.
6. Nominate the plurality winner, hear the final defense and cast a verdict.
   Verify the defendant cannot vote; acquittal and tied verdicts spare them, guilt
   executes only after the configured last words. Test one nomination revote and
   a second tie that skips the day.
7. Inspect the single player panel at 1280x800, 1920x1080 and a narrow window.
   Confirm badges match public claims, dead names are grey/DEAD, and no old claims
   row or horizontal player strip survives. Check all seven rows fit and scroll.
8. Die and let the run continue without touching Watch or another continuation
   button. Confirm it pauses for readable text and stops at the final result.
9. Enable full motion; observe tense and fear-tagged text. Motion must stop after
   1.5 seconds while colors/text remain. Check reduced motion and effects-off too.
10. Play Infinitum: no night action, attacks cannot kill it, voting can, and a normal
    antagonist win becomes Infinitum's win only while it lives. Confirm Town victory
    does not reward it and the Anarchy/Infinitum two-survivor state draws.

Portable tests and static checks do not validate Unity import, UI rendering,
keyboard/touch behavior, TMP meshes in the actual editor, profiling or player builds.
