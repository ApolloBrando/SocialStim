# Milestone 03 role and playtest contracts

## Catalog and setup

The authored nine-slot list contains Town Investigative, Town Protective, Town
Support, two Random Town slots, a Citizen, an Antagonist Killing slot, a Random
Antagonist slot, and a Random Neutral slot. Random slots honor faction/category
constraints, uniqueness, and the requested player role. Impossible setups fail
before creating a session. Explicit scenario construction remains available.

| Role | Night action | Resource and outcome |
| --- | --- | --- |
| Citizen | None | Repeatable Town filler; full day speech/voting agency |
| Inspector | Investigate appearance | Unlimited; appearance is uncertain when forging exists |
| Physician | Protect another | Unlimited basic temporary defense |
| Interrupter | Block another | Unlimited; accepted blocks are simultaneous |
| Vigilante | Independent basic attack | Two charges; blocked/pass preserves a charge, failed attack spends |
| Assassin | Single coordinated team attack | Unlimited basic attack; player killer controls target/pass |
| Forger | Frame another as apparent Antagonist | Unlimited; changes only that night's investigation appearance |
| Coordinator | Watch another's actual visitors | Unlimited; sees identities, not roles or which visit was an attack |
| Survivor | Self-protect | Two charges; personal objective is survival at terminal outcome |
| Provocateur | None | Unique; personal objective requires execution, not a night death |
| Lone Killer | Independent powerful attack | Unique; basic innate defense; eliminate Town and Antagonists |

## Night order

Blocks resolve simultaneously, including mutual blocks. Unblocked forgeries
apply before visits and investigations. Accepted block visits remain visible;
blocked non-block visits do not happen. All attacks resolve as a simultaneous
batch. A dying unblocked actor still acts, but private reports are delivered
only to living recipients. Watchers do not see their own visit as intelligence.
No visitor/forgery report is automatically public.

Apparent alignment differences after a role reveal are not automatically proof
of lying when the public catalog contains a Forger. Directly conflicting claims
and reports still generate attributed evidence and uncertainty.

## Outcomes and bounded stalls

Town wins when no Antagonists or independent killers remain. Antagonists win
when no Town/independent opposition remains, or have parity against Town with
no usable attack, block, or protection. Remaining independent killers postpone
Antagonist parity. One Lone Killer wins when Town and Antagonists are eliminated;
benign Neutrals may remain and meet their separate objectives. Multiple competing
independent killers continue. Empty casts draw.

This is deliberately conservative: reusable protection/block loops and mutual
defense may continue until the configured day cap, then draw. Exhausted Town
combat does not postpone control. Terminal elimination checks precede the cap.
Provocateur execution records a personal achievement while the other factions
continue; a night death does not earn it. These are prototype policies, not a
balance claim.

## Day reporting and UI

Opening discussion can be taken by the player or yielded to one random NPC
without spending a discussion round. After a night death, that opener reacts to
the death. Claimants give morning reports before the next opening choice.
Truthful claimants use their own recorded orders/results; false claimants
fabricate from the public role claim and visible participants. A report is an
assertion, never a public truth flag. The player's automatic report describes
their selection and legitimate results, including passing or being blocked.
Manual investigative assertions and role claims still allow deception.

The choice panel stays hidden until all pending lines and reading pauses finish.
Categories, action families, pages, and Back do not submit gameplay commands.
Claims retain a textual ROLE CLAIM label and a persistent unverified claim
board. Display timing does not change rules or random streams.

Day uses a light warm background with dark text. Night uses a dark background
with light text. Day warmth follows public trial progress, not real elapsed
time. Panel/button frames remain when effects are disabled.

## Verification gate

Shared tests cover forging expiration/blocking, scoped actual visits, blocked
watchers, limited charges, simultaneous independent attacks, setup constraints,
capability outcomes/caps, execution objectives, yielding, morning reporting, and
last-action reporting. CI also compiles against NUnit 3.5.

Still requires Unity: import all new assets, start each selectable role, check
morning claims and reports, confirm palette transitions, inspect frames at
Full HD, browse Back/pages with keyboard and mouse, and ensure choices stay
hidden through typewriter and pause. Check Skip, instant text, effects off,
restart, death, and end screens. No Windows build is claimed.
