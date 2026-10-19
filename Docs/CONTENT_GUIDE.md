# Editing SocialStim

Edits apply when you start a new run. Keep Unity assets and their existing
.meta files together; create and duplicate assets inside Unity.

## Where everything lives

Paths below are relative to the repository root.

| Edit | Location |
| --- | --- |
| Spoken dialogue and conditional variants | Assets/_Project/Data/Dialogue/StarterDialogue.json |
| Character names and active content references | Assets/_Project/ScriptableObjects/Catalogs/StarterCatalog.asset |
| Role names, factions, abilities and resources | Assets/_Project/ScriptableObjects/Roles/*.asset |
| Exact role slots and scalable faction shares | Assets/_Project/ScriptableObjects/Setups/StarterRoleList.asset |
| Participant count, seed, discussion/trial limits, death reveals | Assets/_Project/Scenes/Game.unity → SocialStim → GameBootstrap |
| First-person night-action phrases | Assets/_Project/Rules/Dialogue/NightActionText.cs |
| Report-result sentences, announcements and outcome text | Assets/_Project/Rules/Phases/RunSession.cs → DescribeReport, DeliverReport, Move, FinishIfTerminal |
| Choice titles and short explanations | Assets/_Project/Application/Sessions/GameController.cs |
| Readable phase/action names | Assets/_Project/Application/Input/ActionLabels.cs |
| Setup, status, menu and accessibility text; layout | Assets/_Project/Presentation/Text/TextGameView.cs |
| Participant card labels | Assets/_Project/Presentation/Text/ParticipantCard.cs |
| Fonts and button/panel construction | Assets/_Project/Presentation/Text/TextUiFactory.cs |
| Day/night surfaces and semantic text colors | Assets/_Project/Presentation/Effects/TextPalette.cs |
| Text reveal speed defaults | Assets/_Project/Application/PresentationCues/EffectsSettings.cs |
| Speaker speed variation and reading pauses | Assets/_Project/Application/PresentationCues/SpeechPacing.cs |
| Effect cue overrides | Assets/_Project/ScriptableObjects/Effects/StarterEffects.asset |
| Built-in cue timelines | Assets/_Project/Application/PresentationCues/DefaultCueProfiles.cs |
| NPC reasoning weights | Assets/_Project/ScriptableObjects/Reasoning/DefaultReasoning.asset |
| NPC action selection | Assets/_Project/Rules/Reasoning/SocialNpcStrategy.cs |
| Faction and personal win rules | Assets/_Project/Rules/Victory/CapabilityWinCondition.cs and Rules/Phases/RunSession.cs → PlayerWon |

The JSON contains conversation variants. Specific ability reports are assembled
from templates in the scripts listed above so they retain the actual target,
result and night. Menu copy is currently C# text, not a localization database.
The catalog references one dialogue JSON file; adding a second file does not
automatically load it. Replace the reference or extend the importer explicitly.

## Add or change dialogue

1. Open StarterDialogue.json in a text editor.
2. Edit a line's text, or copy an object into the lines array.
3. Give a new line a unique id. Keep existing IDs when revising wording.
4. Choose a registered intent and a positive integer weight (1–1000).
5. Keep at least one unrestricted line for each intent. The starter set provides
   four distinct unrestricted variants per intent.
6. Run Tools → SocialStim → Validate Starter Content in Unity, then start a run.

Example object to add to the existing lines array:

~~~json
{
  "id": "accuse.cautious.005",
  "intent": "Accuse",
  "text": "I could be wrong, but I'd like to hear more from {target}.",
  "weight": 2,
  "conditions": [
    { "kind": "speakerTrait", "value": "Cautious" },
    { "kind": "phase", "value": "Discussion" }
  ]
}
~~~

Do not include a speaker prefix: the session adds the name once. For ReportAction,
{result} already contains a complete first-person account. Write a short wrapper
or just {result}.

| Variable | Meaning |
| --- | --- |
| {speaker} | Speaker's name; usually unnecessary in text |
| {target} | Selected target's name; use only when this intent has a target |
| {role} | Publicly claimed role name, which may be a bluff |
| {result} | Asserted alignment, or a complete ReportAction account |
| {day} | Current public day number |

Registered intents: Accuse, Defend, ClaimRole, ReportInvestigation, Vouch,
CallForTrial, Agree, Disagree, Deflect, StaySilent, ExpressFear, ReactToDeath,
ReactToRoleReveal, LastWords, VoteJustification, ReportAction.

Supported styles are `<emphasis>`, `<fear>`, `<calm>`, `<evidence>` and `<danger>`, each
with a matching closing tag. Use `<<` for a literal less-than sign. Do not use
TMP/HTML tags. A line's meaning must remain clear with effects disabled.

Conditions combine with AND. speakerRole tests the speaker's real known role;
it does not test a public claim or a target's secret role. An unrestricted line
has no conditions, speakerRole or requiredTrait. Generic accusations should
not invent evidence such as a contradictory vote that never happened.
See [DIALOGUE_AUTHORING.md](DIALOGUE_AUTHORING.md) for all condition names.

## Change names

Select StarterCatalog in Unity. Expand Names and edit or add entries. Every
name must be nonempty and distinct, and the pool must have at least as many
entries as participants. Names are shuffled separately from roles; they do not
define permanent characters or personalities. Personality flags are currently
assigned from the seed in RunFactory.Create.

## Change participant count and role composition

Open Game.unity, select SocialStim and change Participant Count on GameBootstrap.
This includes the player and defaults to 9.

- Positive count: use scalable faction shares from StarterRoleList. At least
  three participants are required. There is no fixed upper player or role-catalog
  count in the rules. Supply enough names and eligible repeatable roles.
- Zero: use the exact Slots array from StarterRoleList. Its length becomes the
  participant count. Add/remove slots in Unity to author a precise scenario.

For scaled casts, Antagonists = floor(count × Antagonist Share / total shares),
with at least one; Neutrals = floor(count × Neutral Share / total shares), with
at least one when the share is positive and count is at least five. Town fills
the remainder. Counts are clamped to leave at least one Town and one Antagonist.
Defaults are 6:2:1: 9 participants gives 6 Town, 2 Antagonists, 1 Neutral;
12 gives 9 Town, 2 Antagonists, 1 Neutral. Set Neutral Share to zero to omit them.

One Antagonist Killing slot is guaranteed. Four or more Town slots include one
Investigative, Protective and Support slot; remaining slots are Random Town.
Smaller Town groups use unrestricted Town slots. Random slots can repeat
non-unique roles. The scaled setup does not guarantee a Citizen; use exact
slots if a particular role must appear.

An exact slot can specify a role ID, a faction filter, a category filter, or a
combination. All enabled constraints must match. A requested player role must
fit a slot without violating uniqueness. Impossible choices fail visibly;
for example, a three-person setup cannot replace its only required killer
with a non-killing Forger. Larger populations need balance/playtesting; a
configurable count is not a promise that every size is fun or fast.

Participant and claim strips scroll horizontally rather than squeezing all
entries into one row. Action menus remain grouped and paged.

## Add a role using an existing ability

1. In the Unity Project window, use Create → SocialStim → Role Definition, or duplicate a
   role asset under ScriptableObjects/Roles.
2. Set a new stable lowercase ID, such as herbalist, and its display name.
   IDs must be unique; changing a display name does not require changing the ID.
3. Choose Faction and a matching Category. Categories affect setup eligibility
   and some victory policies, not just display grouping.
4. Choose an existing Ability. Set Attack, Defense, Protection, Charges,
   Appearance and Unique as appropriate.
5. Add the new asset to StarterCatalog → Roles.
6. Ensure a setup slot can accept it. Scaled setups use faction/category
   constraints; exact lists may require a new slot or an updated filter.
7. Optionally add dialogue with a speakerRole condition matching the ID.
   Generic fallbacks already let the role speak and be claimed.
8. Validate content and test the role in a new run, including death and victory.

Example: duplicate Physician, set ID herbalist, name Herbalist, keep Town /
Town Protective / Protect, Protection 1, Charges 2, Appearance Town, Unique off.
It now shares the protection mechanic with two uses and can fill Town Protective
or Random Town slots. Add an exact herbalist slot to guarantee its presence.

Charges: -1 is unlimited, 0 prevents a night action, a positive number is the
starting use count. Passing or being blocked preserves limited uses. An
executed action spends a use even if its attack fails. None grants no visit;
use Town / Town Benign / None / Charges 0 for a repeatable ordinary good role.
Attack must exceed effective defense to kill. Protect grants temporary defense;
multiple protectors take the maximum. Appearance is what investigations see,
not necessarily the real faction. Unique allows at most one copy per run.

Town roles share the Town objective. Antagonists share teammate knowledge and
one coordinated team kill. Neutral Benign survives to the end. Neutral Evil
currently uses the Provocateur objective (execution); Neutral Killing uses the
Lone Killer objective. A different personal objective requires code.

## Add a new mechanic

A new ability or win condition needs implementation, not just an asset:

- Append new serialized enum values; never reorder existing ones.
- Add command/target legality in NightLegality and player choices if needed.
- Implement an INightStage and register its unique priority in NightResolver.
- Define visits, blocking, finite-charge spending and simultaneous-death behavior.
- Add scoped result kinds and readable private/public report templates.
- Update NPC selection in SocialNpcStrategy and the optional BasicNpcStrategy.
- Update outcome logic when the mechanic changes control or personal objectives.
- Add interaction and information-isolation regression tests.

Use [ROLE_INTERACTIONS.md](ROLE_INTERACTIONS.md) as the current rules contract.
Rules and Application remain independent of Unity.

## Check edits

Run the Unity content menu, then play the edited role/setup. Check actual
rendering and keyboard/mouse navigation at Full HD. Test larger casts with
horizontal scrolling. From a development shell, run:

~~~sh
python3 Tools/validate_scaffold.py
dotnet build Tools/HeadlessTests/SocialStim.HeadlessTests.csproj --configuration Release -p:NUnitVersion=3.5.0
dotnet test Tools/HeadlessTests/SocialStim.HeadlessTests.csproj --configuration Release
~~~

Headless checks do not compile Unity adapters or replace Play Mode testing.
There is no ongoing-run save system; New run always starts fresh.
