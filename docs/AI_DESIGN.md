# Council NPC reasoning

## Boundaries and determinism

CouncilKnowledge is the complete decision port. It holds public identities, claims, votes, reveals and deaths; the observer's role/resources/results; explicitly granted Evil teammate identities; public run-local skills; and the role-list information permitted by Full / Faction Counts / Hidden. It has no CouncilWorld or foreign role reference. Inference rejects a result addressed to someone else.

Hidden lists use a declared broad Town/Evil/Neutral prior (65/25/10), not undisclosed setup counts. Faction-count lists constrain sampled initial totals. Full lists constrain exact initial role multiplicities. Own role, public reveals and known teammate factions are hard constraints. Claims and reports are uncertain evidence because bluffing and forging exist.

Decision randomness uses the run's seeded IRandomSource. Inference has an isolated seed derived from run seed, observer ID and authorized evidence counts. Previewing UI, opening drawers and inspecting beliefs do not consume decision randomness. Public/recipient event sequence numbers are reassigned after filtering, preventing private event-count leakage.

## Bounded hypotheses

CouncilHypothesisSampler draws complete assignments with uniqueness and disclosed counts. CouncilInference evaluates at most 32/80/160/256 accepted samples for Easy/Normal/Hard/Brutal, with at most four times that number of attempts; explicit overrides clamp to 16–1024. Importance weights combine attributed social evidence, role claims and private/public reported readouts. Log weights are normalized before marginalization; effective sample size is exposed for diagnostics.

Readouts remain three-way. CLEAR includes passive Infinitum; Aperture can invert CLEAR/CORRUPT, but not ANOMALY. A plausible bluff describes the reporter rather than authenticating the target. Public night death is incompatible with unqualified night immunity. Reveals rebuild likelihoods over earlier votes, accusations and defenses, so history is re-evaluated instead of permanently applying stale trust changes.

These are bounded model estimates, not calibrated probabilities or exhaustive theorem proving. Finite samples can miss a plausible assignment and importance weights can concentrate. The inspector reports effective samples so overconfidence is visible. No exact hidden assignment is used to repair a bad belief.

## Social evidence and skills

Role/faction probability, credibility and affinity are separate values. Persuasion multiplies stated reason strength, speaker Charisma and Logic/Performance, speaker credibility, personal affinity, listener logic and gullibility/caution. Repeated identical assertions saturate with a power-law discount. There is no fixed suspicion increment per accusation.

Claims that change, incompatible reported results, unique-role counterclaims and revealed voting/defense history change credibility. Affinity reflects personal accusations, defenses and agreement, modulated by Charm. Stealth reduces public threat and shapes silence evidence. Intuition reduces bounded decision mistakes. Death fraction and elapsed days raise public tension and aggression.

Every participant receives a seeded six-skill profile with a 30-point total. NPCs and Random players vary within that budget; Balanced players get six fives; Point-buy players must allocate exactly 30 points, each skill 1–10. Skills and relationships never survive a run. No skill grants access to hidden facts.

## Decisions and faction objectives

The strategy chooses intent, target and reason before dialogue selection. It reports actual previous-night results, claims when threatened, and selects supported reasons by strength. Bluff roles prefer publicly available, uncontested action roles and preserve existing claims. An explicit declared lie is allowed only through role claim/bluff flags and shared validation.

Town ranks hostile probability (Evil plus Anarchy), avoids publicly confirmed Town even on deliberate mistakes, shares critical readings/saves, heals credible threats and investigates uncertainty. Nullifier requires 84–94% hostile confidence depending on difficulty and never shoots revealed Town.

Evil avoids teammates, chooses public power-role/persuader threats, and reruns support choices with the actual authorized team kill target so Singer can avoid a redundant block and Aperture can cleanse a pressured teammate or frame another target. Difficulty scales mistakes and use of coordination forecasts. Teammate bussing is conservatively omitted; this prevents an early accidental team sacrifice.

Anarchy weighs threats, avoids probable Infinitum, and may hold an exposed early-game attack. Infinitum keeps a lower discussion profile and favors Town elimination plus removal of Anarchy to advance its parity clock.

Voting combines faction objective value, personal affinity, public stance support and survival pressure. CouncilVoteForecast predicts fractional ballots from public latest stances, evaluates how one added vote shifts probable plurality/tie exposure, and estimates parity urgency. Current ballots are locked before reveal; nobody reads votes cast earlier in the same resolution. Forecasts are deliberately imperfect; they do not query another NPC's beliefs.

## Diagnostics and performance

Private Decision records retain chosen action, target, reason, estimated hostility and public pressure. They are omitted from gameplay display and knowledge, then included in the after-action report. The AI inspector is compiled into editor/development UI only. It shows observer-local top roles, credibility, affinity, sample/effective counts and exposure. Do not use it when testing human deduction.

CouncilSimulation drives the ordinary validated player port with the same NPC strategy. It reads true roles only after a completed run, for aggregate vote accuracy and role win associations. The batch test runs 500 seeds at each difficulty (2,000 total), nine participants, three discussion rounds, 15-day cap and a documented low sample override of 16. It records outcomes, game length, steal rate, Town vote accuracy, failures and per-role wins/presences. Those associations are not causal role impact estimates.

Inference caches by observer-authorized evidence/day/round. Work is bounded by sample/attempt caps and catalog/cast size. Headless timing is a diagnostic, not a Unity frame-time guarantee; profile supported devices before claiming a frame budget. Test receipts and tuning conclusions belong in docs/AI_BASELINE.md.

## Unity checklist (not run here)

1. Use identical seed/settings/inputs twice; compare every statement, result and vote.
2. Try all difficulties and stat modes. Confirm per-run stats/role-list visibility in My Role, and fresh profiles on New Run.
3. With the development inspector, compare different NPCs after one private reading; only its recipient should gain that information. Try Hidden and Full lists.
4. Accuse using different reasons; compare listener distributions and credibility. Repeat one line/reason: influence should saturate.
5. Reveal a previously defended Evil; earlier defenses/votes should be re-evaluated. A unique-role counterclaim should reduce certainty rather than identify the liar automatically.
6. Play Evil and watch Singer/Aperture support the private team target; no kill should target a teammate.
7. Observe Physician self-heal, cautious Nullifier shots, Anarchy endgames and Infinitum parity steals.
8. Complete a run and inspect decision traces. During a run those traces and foreign private results must remain unavailable outside the developer inspector.
9. Profile inference at 9/16/full-capacity participants and the largest sample override. Record device timings; lower the budget if necessary.
