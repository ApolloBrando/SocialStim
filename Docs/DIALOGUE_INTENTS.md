# Dialogue intent reference

Edit the seven personality documents in `Assets/_Project/Data/Dialogue`: AnxiousDialogue.json, BraveDialogue.json, SlowDialogue.json, ChatterboxDialogue.json, SmugDialogue.json, GrimDialogue.json and JokesterDialogue.json. StarterDialogue.json holds player lines. DIALOGUE_AUTHORING.md explains fields; DIALOGUE_COVERAGE.md gives generated coverage counts.

This pass rewrites 280 of 970 lines for clarity, retaining every ID, condition, weight and metadata field. Each trait retains eight dedicated CounterClaim variants. The voice can vary; the social action must remain clear.

| Intent | What the line must communicate |
| --- | --- |
| Accuse / CallForTrial | Suspect the named target or seek their trial. Suspicion is opinion unless supplied evidence supports more. |
| Defend | Deny the accusation against the speaker. |
| Vouch | Support the named participant. Do not imply knowledge of their secret role. |
| Deflect | Refute the attack on the speaker, then turn suspicion onto the named accuser. Both parts must be explicit. |
| Agree / Disagree | Support or oppose the referenced speaker's preceding statement. The target placeholder identifies that speaker, not necessarily the person being accused. |
| ClaimRole | Explicitly assert the supplied Town special role. |
| CounterClaim | Explicitly contest another claim to the same unique role. Use this pool, not ordinary claim prose. |
| ReportInvestigation / ReportAction | Legacy pools remain for compatibility. Live previous-night reports use structured recorded facts and editable GameText.json templates. Personality prose cannot replace a target or result. |
| ExpressFear | Express concern without inventing a visit, investigation or confirmed alignment. |
| ReactToDeath / ReactToRoleReveal | React to the supplied public event. |
| LastWords / VoteJustification | Give a final position or explain the intended vote without inventing evidence. |
| StaySilent | No spoken line. Skips the dialogue action and advances the round. |

New UI action tags such as [COUNTERCLAIM] and [REPORT] explain intent in addition to colour. Their wording, role descriptions, report sentences and new menu text live in GameText.json; stable keys and named template fields are validated by GameTextDocument/GameText. Compatibility defaults mirror the editable catalog for portable clients.

Keep jokes, hesitations and sarcasm around a plain accusation, denial or support statement. Avoid saying someone visited, lied or used an ability unless the command actually supplies that fact. A claimed report remains an assertion: its wording must describe the account clearly without certifying honesty.
