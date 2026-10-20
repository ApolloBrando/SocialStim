# Editing all dialogue and game text

Unity-native editing is available at Tools → SocialStim → Dialogue and Text Editor.
Search entries, inspect metadata, edit prose, Undo/Redo, validate and save. Canonical
source saves refresh the reference automatically inside Unity. For new entries or
metadata changes, use Open JSON source. See [UNITY_WORKFLOW.md](UNITY_WORKFLOW.md).

Start with [the complete catalogue](DIALOGUE_CATALOGUE.md) to find a line by intent
or stable ID. All current conversational choices are in editable JSON, shared by
players and NPCs. Narration, night-result wording, choice labels, phase/menu text,
settings captions, help and diagnostic review wording use a second editable JSON.
There is no hardcoded fallback prose for these entries.

| Content | Authoritative editable file |
| --- | --- |
| Actual spoken choices and NPC lines | [StarterDialogue.json](../Assets/_Project/Data/Dialogue/StarterDialogue.json) |
| Narration, results, choice labels and interface wording | [SocialStimText.json](../Assets/_Project/Data/Dialogue/Resources/SocialStimText.json) |
| Character/role names | Existing StarterCatalog and role ScriptableObject assets, edited in Unity |

The catalogue is a generated reading reference. Editing a JSON source changes the
game; editing only the catalogue does not. Unity saves refresh it natively; external
edits use Tools → SocialStim → Refresh Dialogue Catalogue or the Python command. Keep stable IDs and `.meta` files.
Changes load on setup/new run; modifying source files does not hot-reload an active run.
The present pool has 70 spoken lines. The 500 player-option / roughly 3,500 total
spoken-line mission targets remain future authoring work. Interface templates do
not count toward those targets.

## Edit or add spoken choices

1. Open StarterDialogue.json and find the desired stable `id`.
2. Change `text` in place, keeping the ID. To add a variant, copy an entry and give
   it a new unique ID, a registered intent and integer weight 1–1000.
3. Keep `{speaker}`, `{target}`, `{role}`, `{result}` and `{day}` only where the
   selected intent has the corresponding authorized value. Do not add a speaker
   name prefix; transcript attribution is its own template.
4. Preserve at least one unrestricted fallback per intent. Conditions, speaker
   role and traits filter availability; copying a conditional line does not make
   it universally eligible. Player choices and NPC selection use the same pool.
5. Regenerate the catalogue and validate content before starting a new run.

[The schema and supported conditions](DIALOGUE_AUTHORING.md) describe all current
fields. The mission's complete archetype/reason/truthfulness/stat metadata and
large dialogue expansion are not implemented yet. Do not add unsupported fields.
Semantic tags are editable style instructions; they never alter facts or rules.
See [expanded effects](EXPANDED_EFFECTS.md) for supported styles. Mission dialogue
should use semantic tags rather than raw effect parameters.

## Edit narration, result wording and labels

SocialStimText.json has `schemaVersion: 1` and an `entries` array:

```json
{
  "id": "choice.accuse",
  "text": "Accuse {target}"
}
```

Entries use named tokens such as `{target}`, `{name}`, `{role}`, `{day}` and
`{result}`. Each entry's available tokens are listed in the catalogue. Reorder or
omit supplied tokens when rewriting wording; introducing an unavailable token
fails explicitly. Braces are reserved for valid token names. JSON needs escaped
double quotes and `\n` for newlines. Keep IDs unchanged: code binds semantic
operations to those IDs. Missing required entries, duplicate IDs/properties,
unsupported fields/versions and malformed tokens fail validation. No alternate
English prose is embedded in code.

Groups include `choice` (labels/explanations), `night.selection` and `night.claimed`
(ability accounts), `report` (private/claimed results), `vote`, `trial`, `death`,
`phase`, `run`, `review`, `button`, `screen`, `roster`, `drawer`, and `settings`.
Character and role names remain in existing data assets. Technical exception
messages and editor/debug tooling are developer diagnostics, outside this copy pool.

A new phrase for an existing action needs only a data edit. A new action, result
kind or game mechanic still needs command/rule implementation and a content key.
The catalog currently loads one spoken JSON asset. Additional unreferenced files
are not loaded automatically; do not split the source without extending loading.

## Verify edits

From the repository root:

```sh
python3 Tools/dialogue_catalogue.py
python3 Tools/dialogue_catalogue.py --check
python3 Tools/validate_scaffold.py
```

CI checks catalogue freshness and required text bindings, builds shared C# against
NUnit 3.5/.NET Framework and runs the behavioral suite on .NET 8. Tests demonstrate
that edited narration, choices, attribution and night reports reach actual output;
command identities stay independent of editable text.

Unity checks remain pending: run Tools → SocialStim → Validate Starter Content,
then EditMode/PlayMode tests. Inspect changed lines, placeholders, wrapping,
first/second-press advancement and mouse/keyboard/touch navigation. Check that
new wording cannot imply knowledge or success the speaker did not receive.
