# Natural trait dialogue follow-up

This content branch follows the separate role and QoL branches. The source review covered all 970 pre-existing player/NPC records. It rewrites 534 NPC lines across all seven traits and every registered speech intent, plus 40 short neutral player templates. It adds 42 conditional execution replies. All 970 pre-existing IDs remain, existing record metadata/conditions/weights and placeholders remain intact, and no data format or runtime text generator was added. Content stays in the existing eight JSON files.

## Editorial decisions

- Agreement/disagreement replies pick up the named speaker's point rather than announce an abstract endorsement. The existing action underline and explicit reply context still identify the originating accusation or defense.
- Self-defense sounds like an answer under pressure rather than a repeated statement about disputing a charge. Counterclaims identify the competing role directly.
- Anxious remains hesitant but is not reduced to an apology on every turn. Brave is firm and protective rather than constantly announcing responsibility. Slow pauses to think without repeatedly forgetting ordinary words or explaining the trait to the room.
- Every Chatterbox line was rewritten. The voice remains talkative and conversational, with less repeated commentary about introductions, sentence length and explanations of explanations.
- Smug keeps dry confidence, Grim keeps restraint, and Jokester keeps nervous humour. Their reactions acknowledge other people rather than attach a random imaginary object to each game action.
- Generic accusation/defense lines do not invent witnessed events, exact hidden information or a specific evidence source the strategy did not supply. Reports retain the actual assertion placeholders and existing structured account system. The prose does not add a truth flag.
- All player templates remain trait-free, short and declarative; registered placeholders and existing punctuation/length restrictions are preserved. An expanded recorded report is still a special case documented in the authoring guide.

## Execution context

Each trait gains three death responses and three reveal responses restricted to the Execution phase and observed death/reveal evidence. The session's seeded optional execution reaction selects these more specific same-trait pools before ordinary fallback lines. The one code adjustment permits those existing passive intents in the authoring phase contract; it does not add an action or let players submit during Execution. Execution-specific responses cannot be selected during ordinary discussion. Public reveal-specific replies are not emitted with reveal-on-death off.

The catalogue has 940 NPC lines and 72 player templates: 1,012 distinct normalized text values. Each NPC trait has at least 134 lines (Brave has 136) and all actions retain same-trait unrestricted fallbacks. DIALOGUE_COVERAGE.md was regenerated. Duplicate and lexical near-duplicate lint passes; that is a useful guard, not a substitute for human editorial judgement or a promise that no two sentences convey a similar idea.

## Verification and editor checklist

The checks exercise JSON loading, actual fallback/selection, all-trait/action coverage, placeholder expansion, player length/neutrality, lexical near duplicates, old IDs and metadata, seeded reproducibility, all-role simulation and the execution/discussion condition boundary. Native Unity import, rendering, speech pacing and subjective dialogue flow still need editor playtesting.

In Unity 6000.3.26f1, try each voice through accusation, defense, agreement/disagreement, claim/counterclaim, a morning report, an execution and a reveal. Check that the reply names the expected speaker, action labels remain underlined, report values substitute completely, and only the speaker's own trait pool is used. Compare reveal on/off executions and repeat a seeded run to confirm stable selection. Read the dialogue aloud and flag awkward transitions with their line IDs for a targeted later edit.

No files were deleted. No README, dependency, asset GUID or serialized field changes.

Final stacked validation: **371 portable C# tests passed, zero failures or skips**, including the 1,100-game scoped-policy benchmark and full-catalogue runs in both voting modes. All four Python validator tests and scaffold/content checks pass. NUnit 3.5/net472 compilation has zero warnings/errors. All 185 C# files parse without syntax errors under C# 9; syntax parsing is not Unity API compilation. Original dialogue IDs, non-text metadata and placeholders were checked against the preceding branch and preserved.
