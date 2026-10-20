# Role edge-case audit

Baseline: main bbfd526, including the actual trees of the merged roster/report and effects work. The audit uses DiscussionRevisionTests.AuthoredRoles(), which reads the shipped role assets rather than the older synthetic test catalog.

## Reproduced and fixed

| Bug | Reproduction | Fix |
| --- | --- | --- |
| Infinitum read Neutral despite its documented Town/CLEAR appearance | Investigate the shipped Survivor asset | Set only the serialized appearance to Town; actual Neutral faction, immunity and win steal remain unchanged; GUID preserved. |
| Neutral investigation was treated like reassuring Town evidence | Deliver a Neutral private finding or publish a Neutral report | Neutral updates the observer's alignment distribution but does not receive the Town suspicion discount. It proves neither safety nor hostility. |
| Several attackers repeated the same survival notice | Archon, Anarchy and Nullifier attack Infinitum together | Emit one recipient-scoped Targeted event, while preserving all visits, individual failed-attack reports and spent charges. |
| Reveals were missing from participant history | Execute any role, then inspect its public record | Attach the revealed participant ID to RoleRevealed. No extra reveal occurs when reveal-on-death is disabled. |

The initial 79 new cases produced 14 failures: one appearance mismatch, eleven missing reveal targets, one duplicated survival notice and one incorrect Neutral suspicion discount. After fixes and further cases, all 98 new tests pass.

## Coverage of the eleven shipped roles

Every row is covered by parameterized pass, legal/self/unknown/exhausted target, block, simultaneous death, investigation, recorded-report and execution/history cases.

| Display name | Stable ID | Additional edge checked |
| --- | --- | --- |
| Decryptor | inspector | Own delivered result survives pipeline death resolution; Infinitum reads Town. |
| Physician | physician | Basic protection versus all three attackers; blocked healing and simultaneous actor death. |
| Solarian | citizen | Cannot target at night or claim/report a special role; passing still progresses the phase. |
| Nullifier | vigilante | Limited charge spends only on execution; defense can stop a basic attack. |
| Watcher | coordinator | A blocked watcher learns no visits; accepted block visits remain observable. |
| Archon | assassin | Antagonist friendly fire rejected; failed attack does not become a death. |
| Singer | interrupter | Accepted blocks still execute while blocked, under the existing simultaneous-block policy. |
| Aperture | forger | Blocked forging suppressed; appearance resets next night without changing true faction. |
| Anarchy | lone-killer | Powerful attack beats basic protection; basic innate defense resists basic attacks. |
| Infinitum | survivor | Multiple attacks cannot kill it; day execution can; Town appearance, passive night action and parity steal. |
| Fool | provocateur | Execution earns its personal objective in both voting modes; night death does not. |

Extra cases cover previous-night report targets after the target dies, passes, bluff account role versus privileged actual action, once-per-night reporting, charge-sensitive Infinitum parity, Anarchy/Infinitum stalemate, and terminal victories before the day cap. Existing claim-permission, setup, information-boundary, seeded-run and resource tests remain in the shared suite.

The audit verifies the implemented prototype contracts. The previously documented mission extensions (three-way readout vocabulary, Nullifier backlash, Archon promotion and full healing changes) remain separate outstanding mechanics; this bug fix does not claim to implement them.

## Validation

- 332 shared portable C# tests passed (234 existing plus 98 new cases).
- Python scaffold/content/boundary checks passed; all four Python validator tests passed.
- NUnit 3.5 compatibility compilation passed with zero warnings/errors.
- No README edits, asset removals or GUID changes.

Unity is unavailable. This is not native Unity compilation or a player build. In Unity, inspect the Infinitum private investigation, one survival notice after multiple attacks, neutral report reactions and each revealed role in its participant history.
