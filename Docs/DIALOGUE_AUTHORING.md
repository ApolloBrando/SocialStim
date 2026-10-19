# Dialogue authoring specification

Status: design contract for milestone 02. No runtime loader/importer or final
dialogue content exists in milestone 00. Milestone 01 may use a small explicitly
marked placeholder dataset to prove the playable loop. All final lines are
written by the project owner; no runtime generative AI is permitted.

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
| `placeholder` | True for engineering sample text; false for final authored lines |
| `conditions` | Registered declarative conditions, combined with AND |
| `variables` | Explicit permitted substitutions used by this line |
| `metadata` | Optional emotion/expression, portrait and audio keys; text UI ignores them |

Condition objects use a registered `kind` plus typed fields, never arbitrary
executable expressions. Initial kinds are `phase`, `speakerTrait`,
`speakerKnownRole`, `hasTarget`, `hasAssertion`, and `observedFact`.
`observedFact` tests only facts available to the speaker. Private target-role
conditions are forbidden unless the speaker legitimately knows that fact.
The importer rejects unknown intent/condition/definition IDs.

This sample demonstrates the proposed schema only. It is not production content
or an implemented importer contract yet:

```json
{
  "schemaVersion": 1,
  "lines": [
    {
      "id": "placeholder.accuse.direct.001",
      "intent": "accuse",
      "text": "PLACEHOLDER: {target}, explain your vote.",
      "weight": 1,
      "placeholder": true,
      "conditions": [
        { "kind": "phase", "value": "discussion" },
        { "kind": "hasTarget" }
      ],
      "variables": ["target"],
      "metadata": { "emotionKey": "suspicious" }
    }
  ]
}
```

Supported initial variables are `{speaker}`, `{target}`, `{role}`, and `{day}`;
`{role}` means the role being asserted in this speech context, not necessarily a
true secret role. Other contexts expose explicit values only when allowed.
Names and substitutions must be escaped for presentation markup. No reflected
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

Importer errors include filename and line ID: duplicate IDs, unknown intents,
conditions or assets, missing/unused variables, invalid weights, malformed JSON,
impossible conditions, and missing fallback coverage. An invalid asset must not
silently ship as an empty dialogue pool. Plain text selection does not parse
future markup, and importing final content must reject leftover placeholders.

## Future semantic markup

No custom markup parser or text effects ship in milestone 00/01. Milestone 05
will define a cheat sheet for semantic tags such as `fear`, `shout`, `whisper`,
and `accuse`, mapped to style assets. The presentation importer validates unknown
tags, nesting, closure, and parameters with line identifiers. Rules keep text
opaque. Reserved tags are planning names, not currently supported syntax.

All key information must remain readable with effects, motion, flashes, and
color distinctions disabled. Effect profiles must never change a line's factual
meaning or reveal a secret through style selection.
