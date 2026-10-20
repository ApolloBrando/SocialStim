# Social deduction AI refinement

Baseline: main at `3df550f1ea305d62c69bd93349c3b1e49fe39e1a`, including the
actual merged discussion/claims implementation from PR 26. Research reviewed on
20 October 2026. This document supersedes older discussion-window and AI notes.

## Design goal

NPCs should form different, revisable opinions, act on their own information,
and pursue their role's objective. Stronger deduction must leave room for
persuasion and deception. Winning by reading secret assignments, repeating the
same accusation, or exploiting the player's slot would undermine that goal.
This is seeded, portable C# with authored dialogue; no runtime LLM is involved.

## Research and its application

These references support design principles, not a claim that SocialStim copies
another game's proprietary AI or reproduces a research system's performance.
Community guides below are first-hand player accounts rather than verified
descriptions of an internal algorithm.

| Reference | Relevant finding | Application and limits |
| --- | --- | --- |
| [Nintendo's Gnosia developer interview](https://www.nintendo.com/jp/topics/article/85fb9bc3-6354-493e-af6a-0337b5127acc) | Kawakatsu describes hidden trust, friendship and attention parameters, and the social irrationality of meetings. | Keep social suspicion, credibility and visible exposure distinct. Personal feelings influence behavior without becoming proof. This does not establish Gnosia's exact equations. |
| [Gnosia developer interview, Dengeki](https://dengekionline.com/elem/000/001/726/1726588/) | The developer discusses constructing convincing AI opponents and character behavior. | Use personality-dependent choices and uncertainty, rather than giving every character one shared conclusion. |
| [Gnosia player mechanics guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2732436486) | Describes trust/amicability, attention, doubt, defense and follow-up actions; the author disclaims complete knowledge. | Agreement should be weaker than a fresh case; give speakers bounded opportunities and let observers remain unconvinced. Exact strengths here are our own tuning. |
| [Finding Friend and Foe in Multi-Agent Games / DeepRole](https://arxiv.org/abs/1906.02330) | Combines a structured hidden-role belief representation with game-theoretic decision methods for Avalon. | Respect constraints and separate inference from action. We do not implement CFR, self-play training or DeepRole's value networks. |
| [Bayesian Social Deduction with Graph-Informed Language Models](https://arxiv.org/html/2506.17788v1) | Uses an explicit probabilistic belief layer alongside agent decisions in hidden-role play. | Condition faction estimates on disclosed totals; preserve competing explanations. Our small dynamic program computes faction marginals from heuristic likelihood weights, not the paper's learned model. |
| [Auditing Belief-Conditioned LLM Agents in Hidden-Information Social Deduction Games](https://arxiv.org/html/2607.10814v1) | Examines agent behavior through scoped information and auditable beliefs/decisions. | Test information isolation, replay, specific behaviors and outcomes separately. Better aggregate results would not by themselves prove better reasoning. No LLM prompting machinery is adopted. |
| [Werewolf Among Us: Multimodal Resources for Modeling Persuasion Behaviors in Social Deduction Games](https://aclanthology.org/2023.findings-acl.411/) | Studies persuasion in human social deduction interactions and provides annotated conversational data. | Preserve attribution and context: an accusation, a corroborating result and an echo are different events. Text-only SocialStim has no vocal or visual deception cues to import. |
| [AvalonBench](https://arxiv.org/abs/2310.05036) | Evaluates language agents in a multi-agent social deduction environment. | Test across roles and seeds, and distinguish mechanical completion from convincing human play. We do not transfer Avalon-specific team-selection rules. |
| [Town of Salem 2 player guide: vote for alignment](https://www.reddit.com/r/TownOfSalem2/comments/1c83qqe/the_importance_of_vote_for_alignment_player_guide/) | Describes nomination pressure as a way to obtain claims and information. | Nomination and conviction have different standards. A pressured or quiet participant is not automatically guilty. Our Solarian cannot claim, so indiscriminate claim-or-die pressure would be inappropriate. |
| [Town of Salem 2 player guide: fake claiming](https://www.reddit.com/r/TownOfSalem2/comments/1c7nlgw/how_to_fake_claim_for_the_new_players_out_there/) | Recommends cover stories compatible with observable role behavior. | Choose legal Town-special cover using own ability and personality, retain it, and fabricate one coherent investigation result per night. The owner's rule forbidding Town bluffs takes precedence over community exceptions. |

The resulting design has three layers: attributed observations, observer-local
beliefs, and faction/role-dependent utility decisions. Dialogue expresses the
chosen action; its phrasing does not secretly grant extra factual evidence.

## Independent minds

Each participant's `BeliefState` owns separate suspicion and trust dictionaries
for every participant. Every other character starts with two independently
sampled seeded floats in **[0,10]**, at 0.001 resolution. Trust is not the inverse
of suspicion: a familiar speaker can still seem dangerous. Self starts at zero
suspicion and ten trust. No relationship persists across runs.

`SuspicionValue` and `TrustValue` carry the actual values through restricted
snapshots. Existing integer `Suspicion` and `Trust` members remain 0–100
compatibility projections for existing consumers. They are not new public UI
meters, and the player never receives another NPC's opinion matrix.

Observer knowledge consists of their own role, delivered private results,
authorized antagonist teammates, public history, catalog definitions and the
initial disclosed faction totals. Unknown dead characters remain in the belief
pool until revealed. No deduction uses their unrevealed death alignment.

Faction marginals are conditioned jointly on remaining faction counts with a
forward/backward dynamic program. Increasing one subject's antagonist weight
therefore affects the remaining available antagonist mass. Each subject sums to
10,000 basis points; aggregate totals agree up to rounding with the disclosed
counts. Known facts remain hard constraints. Role estimates within factions are
still approximate and do not enumerate every legal unique-role assignment.
These are heuristic beliefs, not calibrated real-world probabilities.

## Evidence and resistance to pile-ons

- Repeating the same source/target/direction within a day does not add social
  suspicion again. Public repetition still appears in the attributed history.
- Direct social influence scales by the listener's trust in the speaker.
  Agreement echoes are weaker; long agreement chains are discounted further.
  Aggregate ordinary social influence is capped at 1.2 raw suspicion points per
  target/day/direction. Separate private evidence and contradictions remain stronger.
- Repeated agreements cannot continually pump trust. Repeated private delivery
  and repeated public reveals are idempotent.
- A speaker's own accusation or fabricated report cannot persuade their own
  model that the target is hostile. Their outward story remains recorded.
- Own-role uniqueness conflicts, changed claims, incompatible results, private
  investigations and relevant revealed voting/defense history contribute to a case.
- A Watcher can privately notice that a claimed investigation target conflicts
  with a visitor they actually saw. That private realization does not invent
  public exposure or reveal the visitor's true role.
- Retroactive credibility for earlier accusations is counted per accuser rather
  than per repeated sentence. Mistaken social pressure is weaker evidence of
  deception than an incompatible role/result story.

The policy combines suspicion, count-conditioned antagonist estimates and case
strength. It uses seeded variation for tied targets, rather than lower character
IDs. A weak same-day repeated accusation receives a utility penalty. Neither
name nor player identity grants suspicion, guilt, protection or immunity.

## Decisions and role objectives

Town can nominate an uncertain suspect to obtain a defense, but conviction has
a higher evidence requirement. A high randomized starting suspicion alone cannot
produce a Town Guilty verdict or a Nullifier shot. Strong private evidence,
contradictions or corroborated reports can support those decisions. A private
CLEAR reduces threat; it remains an apparent result because deception exists.
Hard authorized Town knowledge prevents hostile Town decisions.

Town power roles retain cover until public pressure or useful hostile evidence
makes disclosure worthwhile. Investigators prefer unresolved subjects; protectors
consider trust, perceived danger and public power claims; Watchers prioritize
likely important locations. Limited Town attacks require a substantive case.

Antagonists protect known teammates, value public power-role targets, and retain
one coordinated faction kill. Their daytime pressure need not reflect their true
belief about a target. Cover preferences vary by personality and compatibility
with their own visit ability, using only legal unclaimed Town specials. Immediate
counterclaim rules still limit NPC bluff challengers and avoid challenging a
teammate's claim.

A fake investigative story requires an existing public investigative claim.
There is at most one invented result per night, including morning reports.
The liar may manufacture a CLEAR to build credibility or frame an opponent;
they do not publish a hostile result against a known teammate. Reporting visits
to multiple different subjects for one night is treated as a contradiction.

Anarchy operates without teammate knowledge and must outlast other votable
survivors. Anarchy plus Infinitum alone remains the defined stalemate draw;
adding a votable Fool no longer prematurely ends the run. Infinitum favors
pressure consistent with its parity objective. Fool gains provocative pressure
utility toward its execution objective. These are lightweight heuristics, not
multi-turn optimal planners or fully modeled theories of other minds.

## Discussion and voting fixes

Every accepted player action in the default discussion consumes **one round**:
Wait, accusation, defense, refutation, follow-up, claim, counterclaim or passing
the counterclaim window. The old NPC queue and pending response are cleared.
The final round enters voting and cannot reopen through another response.
Deliberately yielding the initial opening is the sole pre-action exception.
The legacy serialized window preference remains accepted for compatibility;
the effective UI reports one action per round.

Each exchange has a seeded bounded group of speakers. Optional NPC follow-ups
have a two-speaker budget, with each listener reacting at most once per exchange.
This permits agreement before the player answers and support after a defense
without unlimited interruption chains. Silence publishes no speech.

Restricted revotes now supply their legal candidate list to the strategy while
retaining the entire public cast in its knowledge. It can reassess the tied
candidates rather than proposing an excluded favorite and accidentally abstaining.
Mandatory-vote fallback uses scoped threat with seeded ties. All ballots are
still chosen before any are published.

`RoleList.ForPopulation` now respects custom low antagonist shares. Ceiling
allocation preserves the default two antagonists at seven without forcing two
when the owner deliberately authors a much lower share.

## Validation and practical limits

Required portable verification on this revision:

- Shared C# suite: **203 passing tests**, NUnit 3.14 / .NET 8.
- NUnit **3.5 / net472 compatibility compilation**, zero warnings/errors.
- Python scaffold/metadata/dependency/content checks passed; four Python
  validator regression tests passed. `git diff --check` passed.
- No Unity import, adapter compilation, editor test execution, player build,
  native profiling or visual verification was available.

The new seven-person benchmark runs 100 seeds for each of the eleven authored
player roles: **1,100 games**, zero rejected commands or stalled games, mean
**5.74 days**, maximum **233 inputs**. Results: Town 472, Antagonist 276, Anarchy
134, Infinitum 56, Draw 162. Maximum day limit is 20. These are fixture results,
not estimates of human win rates: player roles are deliberately sampled equally,
the player policy always defends a direct accusation, and counterclaims use a
fixed 40% choice. It exercises the rules API with a scoped player snapshot,
rather than reproducing the front-end's exact menu policy. Existing controller
simulations additionally cover 176 games in each voting mode with real menus.

In the recorded 200-seed first-day Wait sample, baseline main required 1,539
actions (maximum 26) for five rounds; this revision takes exactly 1,000 (five
each). NPC pressure targeted the player 539/1,508 times before and 104/1,328 after.
Changed pacing changes opportunities, so this comparison is a regression signal,
not proof of fairness or a desired target percentage. A separate equal-target
test samples six symmetric seats over 600 seeds and checks that each receives
65–135 choices, including seat zero. The player receives no special exemption.

The tests also cover fractional initialization, observer isolation, duplicate
evidence, social caps, joint-count conservation, evidence-gated lethal actions,
revote candidates, scoped Watcher inference, legal coherent bluffs and neutral
endgames. Existing replay and hidden-assignment-permutation tests remain active.

### Editor playtest checklist

1. Play several seeds as Solarian, Decryptor and an antagonist. Count five player
   actions, including response and counterclaim passes, before the daily vote.
2. Accuse an NPC and let a second NPC accuse you. Check supporters can speak on
   either side of your defense, with varied speakers and no endless response loop.
3. Compare a quiet first day with a day containing an investigation or conflicting
   unique-role claim. Observe uncertainty early and stronger evidence-driven pressure later.
4. As a deceptive investigator claimant, inspect reports across a whole day for
   one coherent nightly result. Ensure Town never claims a false role.
5. Trigger a tied vote, an acquittal, spectator mode and an Anarchy/neutral endgame.
   Confirm the roster, text effects and automatic progression still feel right.

Further human playtesting should measure understandable mistakes, opportunities
to persuade, excessive passivity and role balance. Future improvements could add
joint unique-role hypotheses, richer visit evidence, explicit multi-turn plans
and designer-facing decision explanations. None is claimed complete here.
No existing asset, serialized field or GUID was removed; READMEs were untouched.
