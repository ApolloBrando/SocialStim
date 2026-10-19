# Mission roles and night rules

Core definitions: Assets/_Project/ScriptableObjects/Roles/Mission. StarterCatalog
references them in Mission Roles; the original Roles catalog remains available
for legacy Trial Mode. Phase 1 provides the tested role/setup/night contracts;
the new council session and screens wire them into play in the following phase.

| Role | Faction/category | Action | Resources/passives | Claim/bluff |
| --- | --- | --- | --- | --- |
| Solarian | Town / Basic | None | Repeatable ordinary crew | Neither |
| Physician | Town / Healing | Cancel one attack on a patient | Unlimited visits; one self-heal by default; learns attacked/safe | Truthful |
| Decryptor | Town / Investigative | Read CLEAR/CORRUPT/ANOMALY | Unlimited | Truthful |
| Nullifier | Town / Killing | Attack 1 | Two charges; fatal Town shot schedules next-night backlash | Truthful |
| Archon | Evil / Killing | One coordinated team attack 1 | Unlimited; unique starting carrier | Bluff |
| Singer | Evil / Distraction | Lull/block | Unlimited; eligible for succession | Bluff |
| Aperture | Evil / Forger | Invert tonight's readout | Unlimited; eligible for succession; CLEAR passive off | Bluff |
| Anarchy | Neutral / Killing | Independent attack 1 | Strict last-survivor objective; ANOMALY | Bluff |
| Infinitum | Neutral / Special | No active ability | All night attacks fail; CLEAR; vote-vulnerable; steals Evil parity | Bluff |

Physician, Decryptor, Nullifier, Archon, Anarchy and Infinitum are unique in the
starter definitions. Solarian, Singer and Aperture may repeat. Per-role max zero
disables a role. Unique roles cannot be forced twice. No role identity string
is checked by night or outcome logic: abilities, flags and objective data drive it.

## Claims

ClaimPermissions is shared by action generation and validation. Truthful claims
require the actor's own action role and CanClaimTruthfully. CanBluff permits
other role assertions and fabricated results; passive Infinitum may bluff.
Solarian has neither permission. Truthful reports must match legitimately
delivered results. Duplicate claims of unique roles are assertions in conflict,
not proof of which claimant lies. The later council ledger records provenance.

## Resolution order

1. Priority 10: accepted lulls apply simultaneously, including mutual lulls.
2. 15: unblocked forgeries invert CLEAR/CORRUPT; ANOMALY is unchanged.
3. 20: actual visits and finite action-charge spending.
4. 30: heal tokens (one per unblocked Physician), then any legacy defense grant.
5. 40: simultaneous attacks in stable actor order; blocked attackers do not visit.
6. 45: authorized watcher visitor notices for reusable/extra role definitions.
7. 50: investigation of the night-local readout map.
8. 60: attacked/safe patient notices, prior-night backlash deaths and scheduling notices.

All unblocked actors act even when killed in the same batch. Route reports only
to living recipients. Targets learn they were attacked without attacker identity.
Physician learns whether the patient was attacked, not which visitor attacked.
Two attacks defeat one heal; two heals cancel two otherwise fatal attacks.
Innate defense/immunity is checked before a heal token is spent. Attack must be
strictly greater than defense (0 none, 1 basic, 2 powerful, 3 invincible);
Infinitum's NightImmune flag prevents even strengths above three.

Self-heal spends its separate once-per-run resource only when it executes.
Passing or being lulled preserves it. A finite shot spends on execution even
if healed or stopped by defense. A fatal Nullifier shot at actual Town schedules
the shooter for the following night's cleanup, not the current batch. This
penalty is not blocked by a lull. Backlash can be disabled. If the game already
ends at the stable boundary, no extra night is invented to apply a penalty.

Aperture cannot forge a Physician's attacked/safe notice: its ability only changes
readouts. Multiple forgeries toggle CLEAR/CORRUPT once per executed forge;
ANOMALY remains ANOMALY. The optional CLEAR passive is a ReadsClear role flag.

## Succession and outcomes

After simultaneous cleanup, if no Evil attack carrier lives, succession selects
the lowest-ID living Promotable Evil when enabled. The session grants that actor
the team's attack duty before checking outcomes. Its original identity remains
in the after-action record. It gives up its support action while carrying the
kill; it does not receive a second simultaneous action.

- Town wins when no living Evil or Anarchy remains, provided Town has survivors.
  Infinitum does not share that win.
- Evil wins at living Evil >= Town with no Anarchy. At least one real Evil must
  live; 0 >= 0 never triggers victory. Combat/protection does not postpone parity.
- Living Infinitum steals that Evil parity win. Evil loses. Default parity counts
  Infinitum as neither side; an option can count it as Town or Evil.
- Anarchy wins only as the sole survivor, never alongside benign neutrals.
- Anarchy + Infinitum alone draws immediately: night immunity and reciprocal vote
  ties otherwise cannot resolve. Only Infinitum or an empty cast also draws.
- Elimination outcomes take precedence over the configured day cap; unresolved
  games draw at the cap. Evaluate only after a complete vote/night batch.
- Optional Fool-style objectives require elimination by vote; night death does
  not satisfy them. Dead Town/Evil allies still share their faction's victory.

## Setup policies

SetupConfiguration/SetupBuilder validate counts, enabled roles, min/max,
player role/faction and explicit slots. Default Classic uses about one Evil per
four participants and zero/one/two neutrals at small/medium/large sizes. It keeps
an Archon slot and includes investigative/healing roles when Town has room.
Remaining slots sample eligible definitions, so not every special role appears.

Choosing a support Evil in a five-person Auto game raises Evil to two so the
required killer still fits. Choosing Neutral adds a neutral slot when Auto
would have omitted it. Manual incompatible counts fail clearly.

Chaos removes guaranteed Town specialists. Minimal uses Solarian/Decryptor and
one Archon, no automatic neutrals: a deliberate small role vocabulary, not a
claim of competitive balance. Custom accepts exact role/faction/Any slots and
role min/max. Invalid combinations cannot start. Full/faction-count/hidden
visibility belongs to both player and NPC knowledge, never just the UI.

## Manual phase-1 checklist

- Import Mission role assets, verify all references in StarterCatalog Mission Roles.
- Validate all nine definitions and inspect claim, passive, unique and resource fields.
- Run MissionCoreRoleTests in EditMode and existing suites.
- In the following council UI phase: play each role; verify self-heal, lull, forge,
  finite charges, backlash, promotion, vote immunity distinction and terminal precedence.
- Try Classic/Chaos/Minimal/Custom at 5, 9, 16 and 20 participants, selected roles
  and factions, disabled killer, impossible min/max and missing names.

Extra roles are deferred until this core is stable; no optional ability is
silently enabled by the mere existence of a legacy role asset.
