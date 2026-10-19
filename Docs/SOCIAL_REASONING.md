# Milestone 02: social reasoning

## Status

Implemented on `milestone/02-social-reasoning`, based on merged milestone 01.
The owner authorized this work while checking the existing scene in Unity.
Unity verification remains open; the new branch does not claim that gate passed.

The five-role catalog, core phase flow, plain-text rendering, and existing input
setup are retained. No visual effects, extra roles, cross-run knowledge, runtime
generative AI, or save format are added here.

## Evidence and credibility

Every observer has a separate `BeliefState` with a chronological `EvidenceRecord`
history. Records identify day, speaker/voter, subject, intent, asserted role or
alignment, verdict, and investigation night where relevant. Public statements
remain assertions. There is no authoritative "this person is lying" field.

The model observes accusations, defenses, vouches, role claims, reported results,
agreement/disagreement, nominations, judgement, deaths, and public role reveals.
Private investigations are accepted only by the actual recipient. Snapshots copy
their histories; later events cannot mutate a strategy's already-delivered view.

- Directly switching role claims or changing a reported result for the same
  target/night damages that speaker's trust and raises suspicion.
- Conflicting reports from different witnesses modestly discredit both. The
  observer does not select a truthful witness by reading their real roles.
- A legitimate private result can more strongly discredit a conflicting public
  report for that night, only inside its recipient's model.
- Repeating the identical reported result remains in the history but does not
  multiply its influence every time.
- Public role reveals let observers reconsider prior claims, reports, and votes.
  Mistaken accusations carry a smaller cost than direct self-contradiction.
- Public observations continue to reach the player in spectator mode; private
  results and future team plans are not delivered to a dead player.

Report checks compare apparent investigation results, not a forbidden true-faction
lookup. Current roles have static appearance. Before adding dynamic Forger-style
appearance changes in milestone 03, retroactive report validation must incorporate
night-specific modifier provenance; a final role reveal alone will not prove a
past investigation report false under those mechanics.

## Role and faction estimates

`SetupKnowledge` contains initial public faction counts, announced in the
transcript. The model combines them with bounded evidence weights and returns
normalized estimates totaling 10000 basis points. These are deterministic
heuristic estimates, not calibrated probabilities or an exact joint posterior
over every legal role assignment.

Known own role, authorized teammate alignment and public role reveals enforce
hard knowledge. Publicly exhausted faction slots receive zero weight elsewhere;
known unique roles are excluded from other candidates. The catalog supplies
candidate role definitions, not a secret assignment list. Within an alignment,
a role claim provides a soft likelihood preference until real knowledge exists.

Unrevealed deaths do not update faction totals from authoritative state. Unknown
dead characters remain part of the unresolved assignment pool. This prevents
counts or confidence changes from leaking hidden death alignments.

Normal text rendering does not show these estimates or other NPCs' private scores.
`KnownCharacter` carries only the current observer's own model. A hidden-role
permutation test swaps other assignments while preserving the public setup and
checks that an uninformed Town observer's initial estimates remain identical.

## Exposure and utility decisions

Self-exposure is an observer's estimate from public accusations, nominations,
judgements, vouches, defenses, and visible contradictions. It does not read other
belief models. A private investigation or private realization that a bluff is
false cannot by itself create public pressure.

The default stateless `SocialNpcStrategy` scores candidate intents using this
knowledge, personality flags, credibility, and estimates. A small seeded utility
variation breaks predictable ties. It can accuse, call for trial, defend, claim,
report, vouch, agree/disagree, deflect, react to deaths/reveals, express fear, or
stay silent. Repeating the same intent/subject is discouraged.

Antagonists and deceptive personalities can bluff. They keep an existing role
claim instead of choosing a different cover every turn. Fabricated reports avoid
contradicting their own already-reported target/night. Exposure makes defense or
deflection attractive. Authorized teammates are excluded from hostile selections.
The original `BasicNpcStrategy` remains available through constructor injection.
Both strategies receive the same restricted knowledge API and remain stateless
across observers; private memory belongs to each observer's separate model.

## Team planning

Each living faction killer submits a legal preference and a priority derived
from permitted knowledge. `TeamNightPlanner` sees only these proposals. It chooses
the target with most proposals, then greatest priority sum, then lowest stable ID.
The lowest-ID killer acts. If the player is a living killer, their explicit
target or pass overrides team preferences and they act. Other killers pass.

Only one faction kill enters the night resolver. The resulting private plan is
available only to antagonist strategies and a living antagonist player's team
message. Town and Neutral snapshots reject it. Tests include two-killer consensus,
player pass override, single-kill resolution, and no Town transcript leak.

## Authoring and tuning

`DefaultReasoning.asset` is referenced by `StarterCatalog.asset`. It exposes
assertion weight, contradiction penalty, aggression/caution/gullibility/loyalty/
deception adjustments, and the public exposure threshold. Assets convert to
immutable settings when a new run starts. Editing the asset does not mutate an
existing run. Older catalogs without a tuning reference use documented defaults.

Dialogue schema 2 adds registered AND-combined predicates described in
`DIALOGUE_AUTHORING.md`; schema 1 remains supported without them. The starter file
has 64 generic and 6 conditional natural prototype lines. The owner may edit or
add variants through data only; keep generic fallbacks,
then run Tools > SocialStim > Validate Starter Content. No scene rewiring is needed.

## Verification and remaining work

The shared headless suite tests actual code, not a second implementation of the
model. It covers evidence credibility, conflicting claims, duplicate evidence,
private/public exposure separation, normalized constraints, unique role exclusion,
traits, consistent bluffs, team routing, schema versions, condition reachability,
anti-repetition, and hidden-assignment permutations. Existing phase/night/player
agency tests and 100 seeded Town/antagonist runs continue to execute.

Unity import, asset authoring experience, input/layout behavior, and play/build
checks remain part of the owner's open editor gate. Passing model tests does not
establish balanced behavior, dialogue variety, or convincing social interaction;
those require authored content and human playtesting. Ongoing-run saving was
cancelled; observer memory exists only for the current run. See
[CONTENT_GUIDE.md](CONTENT_GUIDE.md) for editing dialogue, roles and setups.
