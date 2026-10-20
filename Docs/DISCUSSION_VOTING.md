# Mission Phase 1A: discussion and vote foundation

Branch: `phase/1-ui-interaction`, stacked on `phase/0-audit-fixes` (PR #13).
Historical foundation contract: see [the subsequent interaction/UI overhaul](INTERACTION_UI.md)
for the current view, settings and permission behavior. This first Phase 1 PR changes the rules/application loop and connects it to the
existing Unity view. The complete Chat/Accuse/Defend target-and-phrasing UI and
single-speaker screen are a subsequent Phase 1 slice, not implemented here.

## Design source and boundaries

The [official PLAYISM Gnosia overview](https://playism.com/en/game/gnosia/)
describes a science-fiction discussion/vote game with opposing playable sides
and nightly eliminations. SocialStim uses that broad discussion-to-vote rhythm.
The exact five-round default, one/two player windows, plurality tie policy and
one-sitting runs come from The Mission part two. Gnosia's story loops, persistent
progression, dialogue, character names, artwork and interface assets are not copied.

## Implemented policy

- New runs default to Gnosia mode: five discussion rounds, configurable from
  three through seven, with one or two ordinary player windows per round.
- Each ordinary window permits one player statement/pass and the living NPC
  statements. An NPC accused by the player or another NPC gives one immediate
  defense per window. NPCs still choose ordinary speech with the existing
  knowledge-limited strategy; this is not the future sampled-world AI rewrite.
- An NPC directly accusing the player pauses the remaining queue. Respond offers
  self-defense, counter-accusation, existing role claims, or silence. A response
  resumes the same window and does not consume another ordinary window.
- Yielding the opening remains optional. A direct accusation from that opener
  permits a response while preserving the first ordinary window.
- After the final ordinary window, each living character casts a ballot. All
  selections lock before any ballot is published. The scoped transcript exposes
  each public ballot separately, followed by the tally; the existing view queues
  these for sequential reveal. No actor sees earlier ballots while deciding.
- A unique highest nonzero count wins, even without an absolute majority.
  No stable-ID tiebreaker selects an elimination target.
- If enabled, the first tie receives one revote restricted to tied candidates.
  A second tie ends the day without elimination. All-abstain ends the day without
  a pointless revote. Abstentions can be disabled; illegal player ballots reject
  before state/RNG changes. A required NPC ballot uses a legal scoped fallback.
- VoteReveal is an explicit acknowledgement boundary before last words/elimination
  or a revote. A player cannot issue another ballot or night action there.
- Optional last words precede one elimination. The existing death-reveal option
  independently controls role disclosure. Existing night and victory policies
  run afterward; the new mission role/victory policies are not part of this slice.
- The next dawn resets round/window counters, vote restrictions and response
  state. Dead players remain spectators and cannot speak or vote.
- Trial Mode retains the original nomination/defense/judgement flow. Historical
  tests now select it explicitly, preserving their original behavioral meaning.

## Structure

`DiscussionSchedule` owns ordinary round/window counts. `DiscussionExchange`
owns the bounded speaking queue, once-per-window victim answers, and priority
response suspension. It sees public identities/commands and injected delivery
ports, never cast roles or UI objects. `RunSession` supplies observer-scoped NPC
decisions and delivers speech through its existing evidence/observation route.

`VoteResolver.Plurality` creates a detached `PluralityResult`: winner, tied
candidates, public counts and abstentions. Session orchestration handles the
revote and transitions. `PlayerView` adds only public round/window, response and
ballot eligibility data. GameController produces revision-scoped choices for the
new phases. Phase/cue/command enum members are appended, preserving old values.

The working UI displays round/window and response/vote headings. GameBootstrap
adds voting mode, group discussion rounds, windows, abstention, tie-revote and
last-words inspector options. Its old `_discussionRounds` remains the Trial Mode
setting; the new group-round field defaults to five without rewriting scene YAML
or changing existing serialized fields/GUIDs.

## Remaining work at the time of the foundation PR

The menu/current-line/drawer/Quick/claim-permission work below is now implemented
in [the interaction slice](INTERACTION_UI.md). Stat gates and reason-matched
inference are still pending. The rest of this section describes the original PR.

The old menu/category/pagination structure remains. The immediate response menu
still uses existing claim permissions; data-driven truthful/bluff permissions
and actual-result selection belong to the role/interaction work. There is no
reason-matched phrasing selector, stat gate, Quick mode, current-speaker roster
selection, drawer ledger, vote-screen animation or after-action report yet.
The UI still auto-queues transcript lines rather than implementing two-press
current-line reveal/advance. This PR must not be described as the full UI overhaul.

## Validation

Local `python3 Tools/validate_scaffold.py` and `git diff --check` pass. New shared
tests cover defaults/bounds, one/two windows, priority suspension, opener responses,
bounded NPC answers, plurality without majority, ballot publication before death,
one restricted revote, all-abstain, mandatory ballot legality, last-words/reveal
switches, dawn reset, and seeded termination/replay across three player roles.
GitHub Actions performs actual shared compilation and NUnit execution. Results
are recorded in the PR; local Unity and .NET execution are unavailable.

## Manual Unity checklist (not run)

- [ ] Open this branch using Unity 6000.3.26f1, resolve the existing packages,
      enable Input System and import TMP Essential Resources.
- [ ] Run EditMode and PlayMode tests; verify no Console errors.
- [ ] Inspect GameBootstrap: Gnosia mode, group rounds 5, windows 1. Play through
      five ordinary windows; confirm the vote follows the final exchange.
- [ ] Repeat with three rounds/two windows. Respond to an accusation and confirm
      the remaining NPC queue resumes without consuming another ordinary window.
- [ ] Check keyboard/mouse behavior for response, voting and VoteReveal Continue.
      Each ballot and the tally must display before elimination/reveal.
- [ ] Exercise tied vote/revote, abstentions off, last words off, death reveal off,
      player elimination, spectator continuation, night/dawn and restart.
- [ ] Select Trial Mode; the old discussion-round/trial-limit settings still work.
- [ ] Repeat the same seed, configuration and inputs; compare all observations.
- [ ] Record actual rendering/layout/input results. Headless success does not
      verify TMP/uGUI compilation, device navigation, frame cost or a player build.
