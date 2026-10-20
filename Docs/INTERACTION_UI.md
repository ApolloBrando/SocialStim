# Interaction and UI overhaul

This branch follows the discussion/voting, settings and expanded-effects slices.
It replaces the legacy menu/category/pagination screen and full transcript mesh.
No scene, existing meta GUID, dependency or serialized role asset is rewritten.

## Interaction contract

CHAT / ACCUSE / DEFEND lead to legal roster targets and up to three actual authored
phrasing choices. Selecting a phrasing submits that exact eligible dialogue ID;
rendered previews and delivered words match. Back/navigation do not consume a
turn, change the revision, draw RNG or alter dialogue history. Quick mode chooses
the first offered legal phrasing. A revision cache prevents read-only browsing
from selecting different text. Forged, stale or ineligible IDs fail before mutation.
The primary CLAIM ROLE button is contextual; priority responses replace ordinary
roots with self-defense, counter-accusation, permitted claims and silence.

RoleSpec carries truthful-claim and bluff permissions, with optional RoleDefinition
overrides. Backward defaults allow ability roles to truthfully claim themselves,
and Antagonist/NeutralEvil/NeutralKilling roles to bluff. Ordinary ability-free Town
roles do not receive fake power-role claims. Session legality enforces the same
policy for player and NPC speech. Honest investigation options use the player's
actual previous-night result; the player cannot select a fabricated faction.
Existing role catalog/victory rules remain. This is not the new nine-role design.

Tone labels describe the offered action (Concern, Support, Result, etc.). They do
not invent evidence, implement stat gates, select sampled-world reasons or prove
that a claim is true. Multiple distinct legal intents show one line each up to
three; one intent offers up to three available authored variants.

## Native presentation

The header shows delivered day/phase/round information. The horizontal roster
highlights the current speaker and valid targets; cards carry public life state,
unverified claims and explicitly manual Trust/Suspect/Confirmed marks. The last
view whose observations finished delivery supplies roster state, preventing the
final casualty snapshot from appearing before the queued public reports.

One current speaker/line occupies the reading area. First Space, line click/tap or
Continue completes its reveal; a later press advances. No same-frame second input
can consume another line. Commands unlock only after the pending batch is advanced.
Auto-advance is optional, and pauses under modals. Continuous span effects still
animate after reveal and while choices are available; history is never remeshed.

One information drawer contains delivered history, a dated public claim/report
ledger, the player's own role/objective/uses, help, seed copy, settings, new run and
an after-action report. True roles, collected actual night actions and ballot
suspicion/credibility considerations appear only after terminal resolution. This
is a compact diagnostic review, not a full probabilistic AI explanation trace.
Private results remain observer-scoped. Escape closes modals and focus stays
inside them; buttons retain native uGUI navigation and selected-control scrolling.

The new-run screen retains role/faction choice and a random role, plus Quit.
Preferences persist; current runs, manual marks and transcripts do not.

## Validation and remaining editor work

GitHub Actions passed 173 shared tests and compiled the older NUnit/.NET Framework
surface. Regressions cover selected authored words, read-only navigation, stale and
forged choices, role permissions, truthful results, terminal review scope, and
round attribution, alongside the earlier discussion/vote/effect/settings suites.
Local scaffold and whitespace checks pass. A native TMP/uGUI PlayMode regression
also exercises first-press reveal and second-press advance, but is **not run** here.
Unity is unavailable in this environment. No Unity compilation, rendering, device
navigation, frame-cost measurement or player build is claimed.

Manual checks, pending Unity 6000.3.26f1 and TMP Essential Resources:

- [ ] Run all EditMode/PlayMode tests and validate content; inspect Console.
- [ ] Check setup, all settings groups, drawer and gameplay at 1280x800 and Full HD,
      with UI scale 0.75/1/1.5 and font sizes 16/22/36. Inspect wrapping/scrolling.
- [ ] Navigate actions/roster/phrasing/Back by mouse, keyboard, touch and gamepad.
      Verify focus visibility, scrolling, modal containment and restoration.
- [ ] Complete a line with first press, advance with second; repeat while animation
      continues and with auto-advance, Quick, instant text and settings changes.
- [ ] Play priority responses, sequential ballot/tally reveal, tie/revote, dawn,
      last words, elimination, spectator, Trial Mode and terminal report paths.
- [ ] Confirm claims/marks are assertions, private results remain private, casualty
      state does not precede reports, and true-role review is unavailable mid-run.
- [ ] Restart and check debounced JSON persistence and seed reset; new game settings
      apply only to a new run. Profile warmed current-line effects separately.
