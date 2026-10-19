# Initial council AI baseline

Measured by GitHub Actions on commit `73c137994f743a586b1115035c817eea403a9ff5`, run [39326078915](https://github.com/ApolloBrando/SocialStim/actions/runs/39326078915). Both jobs passed: NUnit 3.5 API compile and **175 portable tests**, including **2,000 completed AI-only games**. Unity was not run.

## Reproduction

CouncilSimulationTests: 500 games per difficulty; seeds 1000–1499 (Easy), 11000–11499 (Normal), 21000–21499 (Hard), 31000–31499 (Brutal). Classic, nine participants, three discussion rounds, one regular player window, default role/night rules, 15-day cap, sample override **16**. The AI controls the player through the same validated command port; priority responses defend self. Dialogue uses deterministic test fixtures so language coverage does not affect this reasoning baseline.

Default interactive sample budgets are 32/80/160/256; this baseline deliberately uses a lower shared budget to keep continuous integration practical. It compares behavior policies under the same inference budget, not the full interactive difficulty-depth settings.

## Results

| Difficulty | Town | Evil | Anarchy | Infinitum | Draw | Avg day | Town vote accuracy | Infinitum steal rate | ms/submitted step |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 148 | 178 | 37 | 128 | 9 | 3.282 | 36.83% | 51.20% | 2.311 |
| Normal | 189 | 156 | 39 | 106 | 10 | 3.288 | 38.16% | 42.40% | 2.112 |
| Hard | 184 | 155 | 47 | 101 | 13 | 3.356 | 37.62% | 40.40% | 2.115 |
| Brutal | 168 | 170 | 54 | 101 | 7 | 3.330 | 36.77% | 40.40% | 2.089 |

Zero failures or step-limit violations in every cohort. Steal rate divides Infinitum wins by games containing Infinitum (250 per cohort), not all games. Vote accuracy counts Town ballots against Evil/Anarchy, evaluated only after the run. Timing includes synchronous NPC resolution per submitted player decision on this headless runner; it is not Unity frame timing.

Aggregate outcomes: Town 689/2000 (34.45%), Evil 659 (32.95%), Anarchy 177 (8.85%), Infinitum 436 (21.80%), draws 39 (1.95%).

## Role win associations (Normal cohort)

| Role | Winning participants / appearances |
|---|---:|
| Solarian | 576 / 1526 |
| Decryptor | 189 / 500 |
| Physician | 189 / 500 |
| Nullifier | 180 / 474 |
| Archon | 156 / 500 |
| Singer | 87 / 240 |
| Aperture | 69 / 260 |
| Anarchy | 39 / 250 |
| Infinitum | 106 / 250 |

Faction allies share a faction outcome even when dead. These are descriptive associations, not randomized causal estimates of role impact. Composition, shared objectives and replacement roles confound them.

## Tuning decision and limits

Retain the initial Classic nine-player balance (six Town / two Evil / one neutral). Both main factions win substantial shares, draws are uncommon, and both neutral objectives occur. The short average is expected at three rounds and a nine-person elimination/night loop. Infinitum remains deliberately disruptive; its 42.4% Normal success conditional on appearing warrants human testing before increasing its frequency.

Difficulty is not expected to order faction win rates monotonically when every side receives the same difficulty change. These results do not establish human win rates, all player counts, Full/Hidden visibility balance, or default-budget frame performance. The initial regression limits are broad; they detect collapse, not small balance changes. Re-run after adding optional roles, Free Time or a substantially different dialogue policy. Human play and supported-device profiling remain required.
