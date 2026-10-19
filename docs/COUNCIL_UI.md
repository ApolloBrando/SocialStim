# Phase 2: focused council interaction

The default scene now starts a main menu and the mission council. The original
TextGameView and RunSession remain reachable only through the Trial Mode
inspector toggle for compatibility. Existing serialized fields/GUIDs remain
unchanged; no scene YAML was edited. A new Council Rounds field defaults to five.

## Flow

Discussion has 3–7 rounds and 1–2 regular player windows per round. A bounded
speaker queue creates exchanges, giving an addressed NPC the next reply. Direct
accusations/questions and unique counterclaims interrupt with a player response;
that priority response does not consume the regular player window. Counter-
accusations receive bounded follow-ups so an adversarial strategy cannot create
an infinite exchange.

After the last window, all ballots are locked before publication. View text
reveals each ballot and tally separately. Plurality eliminates one target.
A tie permits one revote restricted to tied candidates, then no elimination.
Abstention can be disabled. Elimination/reveal and optional last words finish
before night or game over. The whole night resolves atomically; scoped morning
notices are displayed before the next discussion.

CouncilSession coordinates separate day, vote, night, speech, validation and
world services. CouncilController owns portable navigation; CouncilLayout,
CouncilDialoguePlayer and CouncilScreen are Unity adapters. The legacy and
modern sessions do not share mutable run state.

## Interaction and disclosure

Top level is CHAT / ACCUSE / DEFEND, a valid role claim when allowed, and Pass.
Ordinary speech is action → roster target → at most three phrasing choices.
Back and previews preserve revision, RNG and recent-line memory. Quick mode
selects the strongest available supported reason/phrasing without the third step.
Contextual reports use results legitimately delivered to the actor.

One current utterance replaces the transcript wall. Space/click completes the
current line; the next press advances only presentation. It cannot submit a
gameplay action. History, claims, own objective, seed/copy, marks and help live
behind the menu drawer. Visible roster deaths/claims update only after their
records have been delivered. True roles and foreign private action history
are unavailable until game over. Public sequence IDs are rebuilt after
scoping so private event counts do not leak.

The New Run editor and grouped settings are completed in Phase 3. Stats,
inference, relationship evolution and detailed NPC decision explanations are
Phase 4. Free Time remains off by default until its social effects are wired.
The larger reason-tagged content catalog is Phase 5; the initial extended
fallback catalog is not represented as meeting the final volume target.

## Manual Unity checklist

- Open Game.unity with the pinned editor and TMP resources. Main menu, New Run,
  role/faction choices, in-memory Continue and standalone Quit must work.
- Start each core role. Verify five-round default, response prompts, at least
  one regular window per round and hidden invalid claim actions for Solarian.
- Use Chat/Accuse/Defend, roster targets, Everyone, Back, three phrasings and
  Quick mode. Navigation must never consume a turn.
- Click text/Space once to reveal, then again to advance. No input may select
  a newly revealed gameplay choice in the same frame.
- Inspect 1280x800, Full HD and narrow aspects. Roster and drawer button lists
  scroll; one line, speaker and action area must not overlap.
- Test mouse, keyboard, touch/gamepad through the existing Input System module.
  Focus must remain visible and follow scroll.
- Vote, tie/revote, eliminate the player, speak last words, watch after death,
  choose/pass night targets and read all morning notices.
- Claims remain unverified assertions; history/marks must not reveal upcoming
  deaths or role disclosures. After-action roles/orders appear only at game over.
- Toggle the inspector Trial Mode and verify the old scene flow still works.

No Unity editor or build result is claimed by the headless suite.
