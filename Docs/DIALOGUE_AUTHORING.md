# Dialogue authoring guide

The existing schema-2 dialogue format now separates neutral player prose from seven
NPC voices. No runtime generator, new data format, strategy framework or authoring tool
was introduced. All text remains hand-authored, reviewable JSON.

## Files and counts

All files are in `Assets/_Project/Data/Dialogue`:

| File | Audience | Lines |
| --- | --- | ---: |
| `StarterDialogue.json` | Player only; no trait | 72 |
| `AnxiousDialogue.json` | Anxious NPCs | 128 |
| `BraveDialogue.json` | Brave NPCs | 130 |
| `SlowDialogue.json` | Slow NPCs | 128 |
| `ChatterboxDialogue.json` | Chatterbox NPCs | 128 |
| `SmugDialogue.json` | Smug NPCs | 128 |
| `GrimDialogue.json` | Grim NPCs | 128 |
| `JokesterDialogue.json` | Jokester NPCs | 128 |
| **Total** | **898 NPC + 72 player** | **970** |

The catalogue retains its original `_dialogue` reference for player content and adds
seven `_npcDialogue` TextAsset references. Existing references and metadata GUIDs are
preserved. Edit the appropriate JSON directly. `DialogueDocument.ParseMany` combines
these existing-schema documents and validates IDs/fallbacks across files. The native
content validator and build gate use the same GameCatalog loading path.

All 70 original IDs remain: 68 now identify neutral player lines; the two former
personality-gated lines live in BraveDialogue with their conditions/metadata preserved
and their old trait predicates mapped to Brave. The word `aggressive` in one old ID
is a stable historical identifier, not an eighth trait. No dialogue IDs were deleted. The later discussion revision shortens multiword names; see DISCUSSION_CLAIMS_UPDATE.md.

## Single trait and assignment

The existing `Personality` enum is the only trait set: Anxious, Brave, Slow,
Chatterbox, Smug, Grim and Jokester. It is no longer a flags enum. None denotes the
player or an unassigned scenario slot; NPCs cannot retain None in a created run.
Combined or unknown values are rejected.

RunFactory uses the existing SeededRandom and shuffle implementation with a separate
seeded stream. Every bag contains the seven traits once. A new bag is shuffled only
when the previous one is exhausted. A small cast has no repeats; every complete group
of seven NPCs contains all traits. Role/faction assignments never select a trait.
Scenario authors may specify a trait through the existing CastMemberSpec parameter,
but cannot repeat one before the current bag is exhausted or give a trait to slot zero.
The trait is not added to the UI or public roster snapshot.

## Reused behavior parameters

The trait addition reused the existing personality weights. The later discussion revision
restricts deception by faction: personality never grants a Town character permission to bluff.

| Trait | Existing branches reused |
| --- | --- |
| Anxious | Gullible assertion bonus; cautious evidence/vote threshold |
| Brave | Aggressive accusation bonus; loyal support/reluctance to condemn trusted people |
| Slow | Gullible assertion bonus |
| Chatterbox | Existing baseline behavior; verbosity is expressed in dialogue |
| Smug | Aggressive accusation bonus; bluffing still requires a non-Town role |
| Grim | Existing cautious evidence/vote threshold |
| Jokester | Humorous voice; bluffing still requires a non-Town role |

Legacy parameter names such as GullibleBonus remain technical names for existing
weights, not a second trait set. There is no talkativeness parameter or per-trait
default text-effect mapping in this baseline. Those systems were not invented here.
A trait does not establish a faction: all seven can be assigned to any eligible role.

## Voice rules

- Anxious: fragmented, hesitant, apologetic, worried; pressure disrupts fluency.
- Brave: direct, protective, willing to answer and act; fear is admitted without retreat.
- Slow: deliberate pauses, delayed responses, literal phrasing and requests for time.
- Chatterbox: longer winding sentences, digressions and over-explanation.
- Smug: polished superiority, condescension and restrained sarcasm.
- Grim: brief, resigned, dry and fatalistic.
- Jokester: absurd images and jokes that still carry the intended action.

Do not turn a stylistic accusation into fabricated evidence. Most current actions
supply only intent/target/asserted role/result. Write within those facts.

## Existing fields

Every document has `schemaVersion` and a `lines` array. Schema versions 1 and 2 still
parse; version 2 is used by the authored files. A file must have an unrestricted
fallback for each action in every voice represented in that file.

| Field | Meaning |
| --- | --- |
| `id` | Globally unique stable ID; preserve when revising text |
| `intent` | One of the 16 existing SpeechIntent names |
| `text` | Authored text, with registered placeholders only |
| `weight` | Integer 1–1000 |
| `requiredTrait` | Exactly one NPC trait; omit or use None for player-only prose |
| `speakerRole` | Optional constraint on the speaker's own known role |
| `conditions` | Optional schema-2 AND-combined predicates |
| `metadata` | Optional emotionKey, portraitKey, audioKey |

For example, this is one line record to insert into a complete Anxious document:

```json
{
  "id": "npc.anxious.Accuse.013",
  "intent": "Accuse",
  "text": "Um, {target}... could we look more carefully at you?",
  "weight": 1,
  "requiredTrait": "Anxious"
}
```

Do not copy the same text to another ID or trait. A generic player line is never
an NPC fallback. `speakerTrait` conditions, when present, must match requiredTrait.
Names parse case-insensitively; role IDs remain case-sensitive.

## Existing predicates and placeholders

| Condition | Parameter | Permitted meaning |
| --- | --- | --- |
| phase | value: phase name | Reachable current phase |
| minimumDay / maximumDay | number | Inclusive day bounds |
| hasTarget / hasAssertion | flag | Presence of an action target/assertion |
| speakerTrait | value: single trait | Same exact voice as requiredTrait |
| speakerRole | value: role ID | Speaker's own role |
| observedDeath / observedRoleReveal | None | Speaker has observed that public event |
| targetAccused / targetHasClaim | None | Public evidence before the current statement |
| speakerUnderPressure | number: 0–100 | Existing visible-pressure threshold |

Available substitutions remain `{speaker}`, `{target}`, `{role}`, `{result}` and
`{day}`. Asserted roles/results are not certified truth. Do not use `{readout}`,
`{evidence}`, private target roles or arbitrary property expressions. Unknown tokens,
malformed braces, invalid conditions and unsupported fields fail loading.

The current schema has **no reasonType, truthfulness, stat-gate, archetype, cooldown or
requires/forbids fields**. Such fields would be rejected. None were added under this
task's prohibition on new systems/formats. Existing eligibility conditions and the
three-ID recent-line memory are reused. Reason-specific player choices cannot be
implemented honestly without a reason-bearing decision/menu contract.

## Selection and fallback

1. Select the exact `(intent, requiredTrait)` pool. None is player-only.
2. Keep only lines whose own-role and context predicates actually match.
3. Prefer the most specific matching context tier; otherwise use broader matching
   lines, ending in the same trait/action's unrestricted pool.
4. Use the existing integer weights and dedicated seeded dialogue stream.
5. Exclude the speaker's three recent IDs; when all eligible IDs are recent, expire
   the oldest exclusions. This never changes trait, role, day or evidence eligibility.

Missing own-voice content fails validation/loading or selection. It never borrows
from another trait or from neutral player prose. Narrator/system observations do not
pass through these trait pools and are unchanged.

## Player wording and rendering limit

Player templates are plain, neutral and at most 14 words; the current maximum is
8. They contain no effects, exclamation marks, rhetorical questions or personality
metadata gates. Applicable old phase/evidence/pressure gates and presentation metadata
remain intact. The player is not assigned an NPC personality.

The existing UI chooses semantic intent/target, not a particular authored phrase.
There is no CHAT/ACCUSE/DEFEND phrasing menu in this baseline. UI work was explicitly
out of scope, so this task neutralizes the shared dialogue that the player actually
speaks without constructing a new menu or inventing supported reason types.

ReportAction passes the existing recorded account through `{result}`. Its source
template is short, but the injected account can exceed 14 words. Rewriting night
reports or changing disclosure behavior would exceed this task's scope. Other
current substitutions are checked with representative multiword names and role text.

## Validation and coverage

```sh
python3 Tools/validate_scaffold.py --write-dialogue-coverage
python3 -m unittest discover -s Tools -p 'test_*.py'
```

The existing validator now checks all eight documents together and verifies the
freshness of `Docs/DIALOGUE_COVERAGE.md`. It reports every trait/action count and
100-line minimum. Frequent actions require eight lines; every other registered
action requires three. Every pool needs an unrestricted fallback. The current pools
exceed those floors: accuse/defend have 12, most frequent actions 10, vouch 6 and
other actions 5, plus the two retained conditional Brave lines.

Normalized equality and word-bigram Jaccard similarity >= 0.8 flag lexical duplicate
or near-duplicate text across the entire database. This is a review aid, not proof
of semantic originality. NPC authored lines are capped at 40 words. Player lines
are capped at 14 and cannot contain markup/exclamations. Canonical NPC prose uses
no effect markup; this keeps it below the existing sparse-markup ceiling.

Shared C# tests cover actual volume, all actions, voice isolation including fallback,
no substitution residue, name constraints, fair seeded assignment, role independence,
repetition and preserved old IDs/gates. The full-catalogue smoke suite loads every
file. Tools/SocialStim/Validate Starter Content and native build validation load the
same catalogue references. Unity execution remains a separate verification gate.

## Counterclaims and repetition (current discussion revision)

CounterClaim is a separate registered intent, with eight lines in each NPC trait
file and four neutral player templates in StarterDialogue.json. It is used only
in the immediate response window to a just-published role claim. Keep normal
ClaimRole introductions and CounterClaim challenges separate. All original IDs,
conditions, weights and asset GUIDs remain intact.

StaySilent records remain authored for compatibility and coverage, but passing
does not select or publish a line. Silence is genuinely silent in gameplay.

Selection now remembers used lines per speaker and intent across the run. It
prefers unused eligible lines before repeating, moves to broader eligible tiers
inside the same trait when a specific tier has been used, and favors lines with
the fewest run-wide uses. It never discards role/evidence conditions. After all
currently eligible lines have been used it begins another cycle, with a recent
history of twelve lines. A pool of eight still repeats after eight uses; 970
records are divided across voices and actions, not available to every speaker.
