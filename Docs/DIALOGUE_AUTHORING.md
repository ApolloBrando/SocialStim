# Dialogue authoring specification

Status: milestone 01 implements the version 1 format below with 45 explicitly
marked placeholder lines. There is a runtime loader, schema validator and editor
validation command. All final lines are written by the project owner; no runtime
generative AI is permitted. Richer condition expressions remain milestone 02 work.

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
| `metadata` | Optional emotion/expression, portrait and audio keys; text UI ignores them |

Version 1 supports only the two optional eligibility fields above. Missing
`requiredTrait` means no trait restriction; missing `speakerRole` means any role.
Unknown and duplicate JSON properties are rejected, including the planned
`conditions` and `variables` arrays. Do not add unsupported conditions and expect
them to take effect. Intent, target and assertion validity are checked by the
session before selecting text. No target's hidden role enters line selection.

This is the implemented format. A complete document also needs an unrestricted
fallback for every intent; copy the starter document before adding variants:

```json
{
  "schemaVersion": 1,
  "lines": [
    {
      "id": "placeholder.accuse.direct.001",
      "intent": "accuse",
      "text": "PLACEHOLDER: {target}, explain your vote.",
      "weight": 1,
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
| `agree` | Supportive without adding facts; 1 sentence | Referenced statement | 6-8 |
| `disagree` | Skeptical without hidden proof; 1 sentence | Referenced statement | 6-8 |
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
