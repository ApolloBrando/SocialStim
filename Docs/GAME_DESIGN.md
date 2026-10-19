# Initial game design

Status: milestones 01/02/05 are merged. Milestone 03 implements the expanded
catalog, including repeatable Citizen, and the latest playtest flow. Current
role/outcome policies and Unity gates are in [ROLE_INTERACTIONS.md](ROLE_INTERACTIONS.md).
The initial-slice descriptions below retain their historical scope.

## Run defaults

Seven participants including the player; seeded fresh names, personalities, and
roles each run. Player assignment is random by default, with a chosen-role option
for debugging and later run configuration. Only settings survive between runs.
If a chosen role cannot fit the role list, report an invalid setup rather than
silently replacing another slot or breaking uniqueness constraints.

The first slice uses one Inspector, one Physician, one Interrupter, two Random
Town slots restricted to those three roles, one Assassin, and one Survivor.
These five starter definitions are non-unique initially. This list is an
interaction test setup, not a claim of balance. A later nine-character example
can use six Town, two Antagonists, and one compatible Neutral; generator
constraints decide valid combinations. Independent killers enter after their
victory interactions and stalemate policy are specified.

## Day and night

Start on Day 1 Discussion. Discussion offers accusations, defense, role claims,
reports, vouches, agreement/disagreement, and silence. Nomination uses one vote
per eligible living voter and a strict majority of living participants to put
a target on trial. The accused cannot nominate themselves. Failed nomination
advances to night in the first slice; no infinite nomination loop.

A successful nomination leads to Defense, then Judgement. Living participants
other than the defendant choose Guilty, Innocent, or Abstain. Guilty must exceed
Innocent; abstentions are excluded; a tie or all-abstain acquits. Votes are
public when that voting stage closes, in stable character order.

Allow two trials per day and at most one execution by default. Acquittal returns
to nomination if trials remain. Guilty leads to LastWords, Execution, optional
role reveal, and an outcome check. Execution ends the day if the run continues.
There is no second execution that day. Make trial limits and reveal policy
configuration. Dead characters cannot act, vote, or serve as ordinary targets.

NightSelection gathers all legal intents without revealing hostile selections.
The player chooses through the input port; NPCs use the same command validator.
The first slice has no daytime role ability. Death enters spectator mode for the
player; player actions stop while the cast completes the run. The final screen
reports faction result, personal objective, and survival separately.

## Initial night contract

| Stage | Defined behavior |
| --- | --- |
| Lock intents | Validate actor, phase, target, charges, and role-specific restrictions against the selection snapshot |
| Role-block | Compute the blocked set simultaneously from accepted block intents; an actor blocked that night still applies its already accepted block in this initial policy |
| Visits and deception | Accepted unblocked visiting abilities register visits; blocks count as visits; blocked visits do not occur; deception modifies an investigation-only appearance snapshot |
| Protection | Establish temporary defense for the target, without revealing the protector |
| Attacks | Compare attack and effective defense; attack succeeds only when strictly greater; attacks at this stage are simultaneous |
| Investigation | Use the post-deception appearance and registered visits; an investigator killed in this batch still completes an unblocked action, but has no living recipient to report it afterward |
| Cleanup | Apply queued deaths simultaneously, consume resources under the ability's documented rule, expire temporary modifiers, create scoped reports |
| Outcomes | Evaluate the completed batch, then issue dawn observations or the terminal outcome |

This is an initial design policy. Unit tests must pin mutual blocks, blocking
a blocker, several visitors, protection strength, simultaneous attacks, and
deception before implementation expands it. Reordering stages must revalidate
their contracts; arbitrary priority integers are not enough.

Assassin has basic attack and no innate defense. Physician grants basic temporary
defense and cannot self-protect in the first slice. Inspector receives apparent
Town/Antagonist/Neutral alignment, not the true role. Interrupter cannot target
itself; the target learns that an ability was blocked, never who blocked it.
Survivor has two self-protection charges granting basic defense. Exact attack,
defense, resource, immunity, and self-target rules become definition data.

Team kill selection produces exactly one team attack. Multiple killing members
submit preferences to a team plan, not separate uncoordinated kills. Deterministic
team priority and an acting killer resolve disagreement; teammate exposure
requires an explicit strategy justification rather than hidden omniscience.

## Starter catalog

Every role can accuse, defend, claim, lie, share alleged results, nominate, and
vote. These actions create attributed evidence; the interface never turns a lie
into authoritative history.

| Role | Faction/category | Night agency | Day agency and bluffing | Risk |
| --- | --- | --- | --- | --- |
| Inspector | Town Investigative | Inspect apparent alignment | Report, conceal, or fabricate a finding; explain discrepancies | Medium: appearance vs truth |
| Physician | Town Protective | Protect another participant | Claim a save, conceal target, vouch cautiously | Low initially |
| Vigilante | Town Killing | Limited-use independent attack | Threaten action, justify a death, conceal ammunition | High: balance and extra deaths |
| Interrupter | Town Support | Block another ability | Share uncertain block evidence and challenge claims | Medium: simultaneous blocks |
| Assassin | Antagonist Killing | Carry out one team kill | Choose scapegoats, invent alibis, support or distance teammates | Medium: team action arbitration |
| Forger | Antagonist Deception | Change apparent investigative alignment | Frame someone and reinforce misleading reports | High: contradictory evidence |
| Coordinator | Antagonist Support | Observe visits to one target | Feed team intelligence into believable public claims | Medium: visibility filtering |
| Survivor | Neutral Benign | Spend limited self-protection | Negotiate support and choose when to disclose neutrality | Low mechanics; passive-play risk |
| Provocateur | Neutral Evil | No night action | Engineer personal execution without making the goal obvious | Medium: personal outcome timing |
| Lone Killer | Neutral Killing | Independent attack | Pose as Town and exploit faction conflict | High: competing threats/stalemates |

Roles are a ten-definition catalog, not ten mandatory slots in every run.
The first five implemented definitions are Inspector, Physician, Interrupter,
Assassin, and Survivor. All others are milestone 03 work.

## Victory and agency

Town seeks elimination of all hostile threats. Antagonists seek removal of
opposition's ability to prevent control; the full game must account for remaining
killers and limited-use killing abilities rather than assume headcount parity
always ends a run. The initial five-role setup may use documented parity only
because it has no independent Town/Neutral killing ability.

Survivor's personal objective is to be alive when the run ends, regardless of
which major faction wins. Provocateur records a personal achievement only when
executed by judgement; it does not automatically end the remaining factions'
run. Lone Killer seeks elimination of competing lethal factions. The initial
loop has a configurable maximum of 30 days, ending unresolved runs as draws.
Advanced stalemate and capability policies remain deferred. Exact policies
for joint outcomes, all-dead draws, stalled combat, turn caps, and protection
loops must be settled and tested before these advanced roles ship.

Antagonist play must offer controlled kill selection, misleading reports,
alibis, suspicion redirection, and teammate coordination. Neutral and Town play
must offer consequential disclosure decisions. The acceptance test is whether
the same system supports credible deception from both sides, not whether the
NPC always obeys an accusation or the player simply survives a vote.
