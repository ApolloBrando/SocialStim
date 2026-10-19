# Dialogue authoring specification

Status: milestone 03 supports version 2 below with 54 explicitly
marked placeholder lines. Version 1 still loads without conditions. There is a runtime loader, schema validator and editor
validation command. All final lines are written by the project owner; no runtime
generative AI is permitted. Conditions are registered data predicates, never code.

## Format

Use hand-authored UTF-8 JSON under `Assets/_Project/Data/Dialogue`. JSON supports
quoted text, structured conditions, validation, and version control without CSV
escaping/column growth. ScriptableObjects hold role/setup/style definitions;
the dialogue importer produces validated plain records for runtime selection.
Adding a line requires no code change. Adding a new semantic action may require
its own command/AI implementation; content cannot invent new game mechanics.

Each file has a `schemaVersion` and `lines` array. Each line has:

| Field | Meaning |
| --- | --- |
| `id` | Stable unique identifier, never reused for different content |
| `intent` | Registered speech intent identifier |
| `text` | Authored text; opaque to Rules |
| `weight` | Positive integer selection weight |
| `requiredTrait` | Optional speaker flag or comma-separated flags; all are required |
| `speakerRole` | Optional speaker's own known role ID for eligibility |
| `conditions` | Optional version 2 array of AND-combined registered predicates |
| `metadata` | Optional emotion/expression, portrait and audio keys; text UI ignores them |

Version 1 supports only the two original optional eligibility fields above. Missing
`requiredTrait` means no trait restriction; missing `speakerRole` means any role.
Unknown and duplicate JSON properties are rejected. A `conditions` array requires
`schemaVersion: 2`; `variables` arrays remain unsupported because substitutions
are validated directly in text. Intent, target and assertion validity are checked
before selection. No target's hidden role enters the context.

This is the implemented format. A complete document also needs an unrestricted
fallback for every intent; copy the starter document before adding variants:

```json
{
  "schemaVersion": 2,
  "lines": [
    {
      "id": "placeholder.accuse.direct.001",
      "intent": "accuse",
      "text": "PLACEHOLDER: {target}, explain your vote.",
      "weight": 1,
      "conditions": [
        { "kind": "phase", "value": "Discussion" },
        { "kind": "hasTarget", "flag": true }
      ],
      "metadata": { "emotionKey": "suspicious" }
    }
  ]
}
```

Supported variables are `{speaker}`, `{target}`, `{role}`, `{result}`, and `{day}`;
`{role}` means the role being asserted in this speech context, not necessarily a
true secret role. Other contexts expose explicit values only when allowed.
The current text view has rich text disabled. Future markup views must escape
substitutions before interpreting styles. No reflected
property access or arbitrary string-to-code evaluation is permitted.

## Version 2 conditions

Each condition has `kind` and only the parameter shown below. Predicates use
speaker-authorized context before the current statement is published. Thus a
line cannot qualify for `targetAccused` merely by delivering its own accusation.

| Kind | Parameter | Meaning |
| --- | --- | --- |
| `phase` | `value`: phase name | Current public phase; must be reachable for this intent |
| `minimumDay` | `number`: integer >= 1 | Inclusive lower day bound |
| `maximumDay` | `number`: integer >= 1 | Inclusive upper day bound |
| `hasTarget` | `flag`: boolean | Matches whether the intent supplies a target |
| `hasAssertion` | `flag`: boolean | Matches a role claim or asserted investigation result |
| `speakerTrait` | `value`: trait flags | Requires all named speaker traits |
| `speakerRole` | `value`: catalog role ID | Tests the speaker's own known role |
| `observedDeath` | None | Speaker has observed a public death |
| `observedRoleReveal` | None | Speaker has observed a public role reveal |
| `targetAccused` | None | Target was previously accused/called for trial/deflected toward |
| `targetHasClaim` | None | Target has publicly claimed a role |
| `speakerUnderPressure` | `number`: 0-100 | Speaker's estimate of visible pressure reaches the threshold |

Phase/trait/kind names parse without case sensitivity; role IDs are case-sensitive.
Day bounds are inclusive. Investigation reports cannot occur before Day 2.
Accusations and other discussion intents cannot select a NightSelection line;
defense supports Defend/ClaimRole, and LastWords supports its own intent.

Impossible day bounds, phase/intent combinations, presence requirements and
conflicting own-role predicates fail at import with the line ID. Unknown kinds,
wrong parameter types, unsupported versions, and unknown catalog role references
also fail. Keep an unrestricted fallback for **every** intent: no trait, own-role,
or condition restrictions. A conditional-only intent pool is invalid.

## Writing by intent

Counts below are starting totals per intent for a small cast, not per role or
trait. Add variants after playtests expose gaps. Keep generic fallback lines for
each usable intent; do not require an exact personality/role match for every
line. Fallback selection must not invent facts or reveal hidden information.

| Intent | Tone and length | Required context | Initial variants |
| --- | --- | --- | --- |
| `accuse` | Direct or skeptical; 1-2 sentences | Target; evidence category when mentioned | 8-12 |
| `defend` | Reactive or composed; 1-2 sentences | Accusation/defense context | 8-12 |
| `claimRole` | Clear assertion; 1-2 sentences | Claimed role ID, truthful or deceptive | 6-10 |
| `reportInvestigation` | Specific and fallible; 1-2 sentences | Asserted target/result/day | 6-10 |
| `vouch` | Qualified confidence; 1 sentence | Target and stated basis if mentioned | 6-8 |
| `callForTrial` | Decisive; 1-2 sentences | Eligible target | 8-12 |
| `voteJustification` | Brief explanation; 1 sentence | Vote, target, known or alleged basis | 6-8 |
| `agree` | Supportive without adding facts; 1 sentence | Public target being supported | 6-8 |
| `disagree` | Skeptical without hidden proof; 1 sentence | Public target being challenged | 6-8 |
| `deflect` | Evasive or counteraccusing; 1-2 sentences | Current accusation, optional counter-target | 8-12 |
| `staySilent` | Short authored silence indication | Speaker's deliberate silence intent | 5-8 |
| `expressFear` | Restrained or alarmed; 1-2 sentences | Public danger context | 5-8 |
| `reactToDeath` | Grief, fear, or suspicion; 1-2 sentences | Observed death | 5-8 |
| `reactToRoleReveal` | Surprise or reassessment; 1-2 sentences | Publicly revealed role | 5-8 |
| `lastWords` | Characterful, not omniscient; 1-3 sentences | Condemned speaker | 8-12 |

Personality tags influence phrasing, not supernatural access to facts. Aggression
can make an accusation blunt; caution can qualify a report; gullibility can
make a vouch overconfident. Avoid every cautious character repeating one hedge.
The same intent can support sincere, mistaken, and deceptive assertions.

## Selection and validation

The reasoning strategy first chooses an intent, target, and assertion. Selection
filters lines by permitted context, then uses integer weights and a dedicated
seeded dialogue stream. Save recent IDs per speaker. When all eligible lines
are recent, relax the oldest repetition exclusion before using a valid fallback.
Never relax a condition that protects knowledge or factual eligibility.

Importer errors include line IDs when available: duplicate IDs/properties,
unknown intents, unsupported fields, unknown role conditions, unknown variables,
invalid weights, malformed JSON, unsupported schema versions, and missing
fallback coverage. Document-level errors cannot always identify a line. An invalid
asset fails loading rather than silently creating an empty pool. Run the editor
validation command before play. Weights are integers in [1, 1000].

Every engineering starter line visibly begins `PLACEHOLDER:`. Replacing these
with final dialogue is owner-authored content work; a final-content build gate
forbidding leftover placeholders is deferred. Selection does not parse markup.

## Presentation boundary

Milestone 00/01 treated all markup as opaque text. The independent milestone 05
branch adds the exact supported syntax below. Planning names such as shout,
whisper and accuse are not supported tags. Rules keep text opaque; rendering
syntax never modifies intent or evidence.
No custom markup parser or text effects ship in milestone 00/01/02. Milestone 05
will define a cheat sheet for semantic tags such as `fear`, `shout`, `whisper`,
and `accuse`, mapped to style assets. The presentation importer validates unknown
tags, nesting, closure, and parameters with line identifiers. Rules keep text
opaque. Reserved tags are planning names, not currently supported syntax.

All key information must remain readable with effects, motion, flashes, and
color distinctions disabled. Effect profiles must never change a line's factual
meaning or reveal a secret through style selection.

## Milestone 05 semantic markup

On the independent 05 branch, presentation parses `<emphasis>`, `<fear>`, `<calm>`,
`<evidence>`, and `<danger>` with matching closing tags. Use `<<` for a literal
less-than sign. Raw TMP/HTML tags are rejected. Rules and dialogue selection
continue treating the text as opaque; markup never changes evidence or intent.
The editor content validator checks syntax, and runtime has a literal fallback
for malformed display text. See `Docs/EFFECTS.md` for the complete cheat sheet.

## Recorded night-action reports

Milestone 03 adds ReportAction alongside ReportInvestigation. ReportAction is
available from Day 2 and substitutes the attributed report into {result}. A
recorded pass has no target; a targeted action may refer to self or someone who
died. hasTarget true/false conditions both work for this intent, and hasAssertion
is true. Keep an unrestricted fallback. The committed catalog now has 54
placeholder lines.

The player chooses the automatic report through Report last night's action.
This discloses their own recorded selection and legitimately received results.
Morning role claimants speak before the next player opening; false claimants
may fabricate from their public claimed role. Presentation labels and the
unverified claim board do not certify an assertion as true.
