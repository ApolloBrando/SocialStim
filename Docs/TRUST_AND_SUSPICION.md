# NPC trust and suspicion

Each NPC keeps its own directed opinion of every participant. A trusts B differently from how B trusts A. Trust and suspicion are independent floating-point values clamped to 0–10; both begin at independent seeded random values across that range. The observer's own trust is 10 and suspicion is 0. The player seat gets no special suspicion bonus.

Trust measures how much weight the observer gives someone's statements. Suspicion measures perceived danger. Neither is a hidden-role reveal, nor is trust the inverse of suspicion. Someone can be persuasive and suspicious at once. Separate normalized role/faction estimates track hypotheses constrained by setup knowledge and actual private/public evidence.

## What changes opinions

- Accusations/refutations add pressure to their subject; defenses reduce it. Agreements and disagreements resolve a current-day statement's subject and direction; yesterday's statements remain historical evidence but cannot serve as an unlabelled new reply target. Repetition is discounted: each source/subject/direction counts once per day, later echoes are weaker, and ordinary social pressure is capped at 1.2 suspicion per subject/direction/day for each observer. This cap does not suppress independent reports or contradictions.
- Trust and personality affect the weight of statements. Contradictions normally add 2.4 suspicion and remove 2.4 trust with the authored tuning. Private evidence can expose a report conflict; the AI does not consult a hidden true role to decide whether a public claim is false.
- Private Sheriff readings strongly affect suspicion (+6.5 for an apparent Mafia reading, -4.5 for Town). Public readings have smaller credibility-dependent effects. Forgery remains possible, so even a private reading is not omniscient role knowledge. Lookout visits to casualties provide leads, not proof of murder.
- Newly improved: a report matching an observer's own investigation of the same target on the same night earns its author +0.8 trust, once per author/target/night. A different night, conflicting reading, repeated report or another NPC's unverified agreement cannot earn this bonus. It never reveals the reporter's role.
- A surviving claimant to a unique role attracts +1.2 suspicion and -0.6 trust after a rival's night death. A public reveal confirming the dead claimant increases these to +3/-1.5. A reveal exposing the dead claimant as a liar cancels that inference. This is deduplicated and excludes dead rivals and known Town.
- Newly improved: an observer who is itself the surviving contested claimant recognizes its own visible exposure (+12, or +30 with a confirming reveal, on a separate 0–100 scale). It does not become suspicious of itself. Reprocessing the casualty cannot add exposure again.
- Public role reveals let NPCs reevaluate past accusations and judgement ballots: correctly opposing revealed Mafia improves credibility; supporting them lowers it. Accusing Town carries a smaller penalty because honest mistakes are possible. Abstentions carry no retrospective adjustment.
- Mafia know allied roles and recorded night actions, which can contradict a public story. Town and Neutral observers never receive that private team information.

## How decisions use these values

Threat combines suspicion, estimated Mafia likelihood and evidence strength. In current portable scoring it is approximately `4 × suspicion + 0.6 × estimated Mafia percent + 0.3 × case strength`, minus 25 for a private Town reading, clamped to 0–100. Known Town and Mafia teammates are excluded from ordinary Town suspicion targeting; known enemies receive maximum threat.

Town judgement requires evidence thresholds as well as threat. Random initial dislike alone cannot justify a guilty vote. Contradictions, investigation readings, visitor evidence, competing claims and the history of supporting revealed enemies contribute to case strength. Personality, role goals, self-exposure, previous targets and seeded tie-breaking influence action selection. Mafia can intentionally lie or vote strategically rather than follow sincere suspicion.

The model is deterministic for identical seeds and accepted actions. There is no general passive daily opinion decay. It remains a heuristic social simulation; the portable seeded-game checks establish repeatability and legal behavior, not human-level deduction or balanced win rates.

## Authoring and implementation

- `Assets/_Project/Rules/Reasoning/BeliefState.cs`: observer-local memories and updates.
- `Assets/_Project/Rules/Reasoning/BeliefDistribution.cs`: role/faction hypotheses.
- `Assets/_Project/Rules/Reasoning/SocialAssessment.cs` and `SocialNpcStrategy.cs`: evidence and action selection.
- `Assets/_Project/ScriptableObjects/Reasoning/DefaultReasoning.asset`: authored personality/influence tuning.

The removed relationship debug window was a privileged viewer, not the source of these opinions. Removing it leaves the gameplay model active. The separate night-action debug tool remains available in developer builds.
