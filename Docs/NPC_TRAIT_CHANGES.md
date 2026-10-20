# NPC uniqueness change record

Baseline: main `90798dd` (includes PR #24). Scope: traits, authored names and player/NPC
dialogue, existing selector/importer/validator/tests/guides. README files are untouched.

## Batches

1. Replaced combinable old trait flags with the seven requested traits; fair seeded
   assignment; expanded the existing name array from 20 to 60.
2. Rewrote 68 existing shared lines as neutral player templates, retaining IDs,
   weights, placeholders and applicable gates. Reassigned the two old personality-gated
   lines to Brave, keeping their IDs and contextual conditions.
3. Authored 120 Anxious and 120 Brave lines.
4. Authored 120 Slow and 120 Chatterbox lines.
5. Authored 120 Smug and 120 Grim lines.
6. Authored 120 Jokester lines.
7. Split the same schema across one player/seven trait files; enforced voice-pool
   boundaries, extended existing coverage/source validation and regression tests.

Final authored total: 842 NPC lines, 68 neutral player templates, 910 globally unique
text records. Source word limits: NPC <=40, player <=14 (current player max 8).

## Names

| Group | Names |
| --- | --- |
| Conventional/uncommon (20, retained) | Ada, Bram, Celia, Dorian, Elena, Felix, Greta, Hector, Iris, Jonas, Kira, Lucian, Mara, Nico, Orla, Petra, Quinn, Radu, Sabina, Tomas |
| Unusual/science fiction (20, new) | Aster Venn, Bex Marrow, Cyr Olan, Dax Meridian, Edda Rook, Fen Vesper, Galen Vox, Hessa Tarn, Ivo Kestrel, Juno Vale, Kes Ardent, Luma Sorn, Miro Thane, Nessa Vey, Olin Strake, Perrin Lux, Rhea Solace, Sable Tern, Ulan Crest, Zev Orison |
| Non-serious (20, new) | Admiral Waffle, Bean Counter, Commodore Socks, Duke Noodle, Eggbert, Fizz Bucket, Gravy Jones, Hat Inspector, Ink Biscuit, Jelly Admiral, Kettle Pop, Lunchbox, Mop Regent, Nugget Supreme, Oat Baron, Plum Socket, Queen Turnip, Rivet Muffin, Sir Breadcrumb, Toast Marshal |

All are unique, ASCII letters/spaces and at most 16 characters. No name was removed.
Initials necessarily repeat in a 60-name pool; each group spreads names across 20
initials. Names describe neither a trait nor a faction. The existing seeded name
shuffle/selection is unchanged; only the authored pool grows.

## Rewrites, migrations and removals

- All 70 old line IDs survive. The 68 player records are rewritten in place with
  their exact old placeholders, weights, metadata and applicable gates preserved.
- `starter.social.accuse.aggressive` moves to BraveDialogue, with Aggressive -> Brave
  and its phase/targetAccused predicates retained.
- `starter.social.vouch.claim` moves to BraveDialogue, with Loyal -> Brave and its
  targetHasClaim predicate retained. Both keep their tense metadata and weight 3.
- Former Aggressive/Cautious/Gullible/Loyal/Deceptive enum labels and combinable
  assignment are removed. Existing behavior-weight branches map to the single new
  trait set; the weight values and formulas do not change.
- Generic NPC fallback is removed. Omitted requiredTrait now means player-only.
- No content IDs, names, source files, existing GUIDs, roles, settings or effects are
  deleted. Splitting moves NPC rows out of StarterDialogue into trait files.
- No narrator/system text, role rule, UI flow or text-effect mapping is rewritten.

## Existing-system limits

Reason/truthfulness/stat/cooldown metadata and exact-phrase UI steps do not exist in
this baseline. They were not invented. Lines therefore avoid unsupported specific
vote-history, claim-conflict or hidden-fact claims. The two existing conditional NPC
lines retain the public-evidence gates they already had. ReportAction still inserts
a recorded account through `{result}` and may exceed 14 rendered words. The guide
explains these limits and the existing behavior mapping.

## Manual Unity checks

1. Open the pinned Unity version, compile and run existing EditMode/PlayMode tests.
   Run Tools/SocialStim/Validate Starter Content. No Unity execution occurred here.
2. Inspect StarterCatalog: original player TextAsset plus all seven NPC TextAssets;
   inspect each JSON independently and confirm the catalogue loads them together.
3. Start a run with at least eight participants. In the debugger, inspect each NPC's
   existing Personality: all seven before a repeat; slot zero is None. Repeat a seed
   and compare, then vary roles/factions while holding seed/cast slots fixed.
4. Read several rounds and accusations/defenses/claims. Anxious hesitates, Brave is
   direct, Slow pauses, Chatterbox rambles, Smug condescends, Grim is flat and Jokester
   jokes while still performing the action. No NPC should speak neutral player prose.
5. Make player statements, claims and recorded reports; check neutral wording,
   intact substitutions and unchanged menu flow. Long recorded reports are the noted
   expansion exception. Verify response and last-words paths as well as discussion.
6. Restart, test a cast smaller than seven NPCs and a larger cast, then edit one trait
   file and reload. Verify no missing references and no unexpected UI/effects changes.
