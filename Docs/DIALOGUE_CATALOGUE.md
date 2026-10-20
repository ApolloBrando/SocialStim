# Complete dialogue and game-text catalogue

Generated from the editable JSON files. Edit the source, then run
`python3 Tools/dialogue_catalogue.py`. CI checks this document stays synchronized.

Current content: **70 spoken lines** and **303 narration/interface templates**.
Templates are not additional conversational variants and do not count toward the 500 player-line target.

| Edit | Source |
| --- | --- |
| Spoken lines shared by players and NPCs | [StarterDialogue.json](../Assets/_Project/Data/Dialogue/StarterDialogue.json) |
| Narration, night results, choice labels, menus and help | [SocialStimText.json](../Assets/_Project/Data/Dialogue/Resources/SocialStimText.json) |
| Editing instructions | [DIALOGUE_GUIDE.md](DIALOGUE_GUIDE.md) |

## Spoken lines

Eligibility still depends on the current role, phase and command. A line listed here is not necessarily available now.

### Accuse (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Accuse.001` | &lt;danger&gt;{target}, I don't trust you yet.&lt;/danger&gt; | {"weight": 1} |
| `starter.Accuse.002` | Something about {target} worries me. | {"weight": 1} |
| `starter.Accuse.003` | Can we hear more from {target}? | {"weight": 1} |
| `starter.Accuse.004` | I think we should take a closer look at {target}. | {"weight": 1} |
| `starter.social.accuse.aggressive` | {target}, people are asking questions. Answer them. | {"conditions": [{"kind": "speakerTrait", "value": "Aggressive"}, {"kind": "phase", "value": "Discussion"}, {"kind": "targetAccused"}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

### Agree (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Agree.001` | I'm with {target} on this. | {"weight": 1} |
| `starter.Agree.002` | {target} has a point. | {"weight": 1} |
| `starter.Agree.003` | That makes sense to me, {target}. | {"weight": 1} |
| `starter.Agree.004` | I think {target} is right. | {"weight": 1} |

### CallForTrial (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.CallForTrial.001` | I want {target} on trial. | {"weight": 1} |
| `starter.CallForTrial.002` | {target}, we need some answers. | {"weight": 1} |
| `starter.CallForTrial.003` | Let's put {target} on trial and hear them out. | {"weight": 1} |
| `starter.CallForTrial.004` | I think it's time to question {target} properly. | {"weight": 1} |

### ClaimRole (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.ClaimRole.001` | &lt;evidence&gt;I'm the {role}.&lt;/evidence&gt; | {"weight": 1} |
| `starter.ClaimRole.002` | I should tell you: I'm the {role}. | {"weight": 1} |
| `starter.ClaimRole.003` | For the record, my role is {role}. | {"weight": 1} |
| `starter.ClaimRole.004` | I've kept this quiet, but I'm the {role}. | {"weight": 1} |
| `starter.social.claim.trial` | Before you decide, you should know I'm the {role}. | {"conditions": [{"kind": "phase", "value": "Defense"}, {"flag": true, "kind": "hasAssertion"}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

### Defend (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Defend.001` | &lt;calm&gt;Give me a moment to explain.&lt;/calm&gt; | {"weight": 1} |
| `starter.Defend.002` | You're making a mistake about me. | {"weight": 1} |
| `starter.Defend.003` | I know you're worried. So am I. | {"weight": 1} |
| `starter.Defend.004` | Please hear me out before you decide. | {"weight": 1} |
| `starter.social.defend.pressure` | I can feel everyone turning on me. Please, let me explain. | {"conditions": [{"kind": "speakerUnderPressure", "number": 45}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

### Deflect (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Deflect.001` | Why aren't we asking {target}? | {"weight": 1} |
| `starter.Deflect.002` | Ask {target} those questions too. | {"weight": 1} |
| `starter.Deflect.003` | {target} has some explaining to do as well. | {"weight": 1} |
| `starter.Deflect.004` | We've barely heard from {target}. | {"weight": 1} |

### Disagree (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Disagree.001` | I'm not convinced, {target}. | {"weight": 1} |
| `starter.Disagree.002` | I see it differently, {target}. | {"weight": 1} |
| `starter.Disagree.003` | I can't go along with {target} on this. | {"weight": 1} |
| `starter.Disagree.004` | Hold on, {target}. I'm not sure about that. | {"weight": 1} |

### ExpressFear (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.ExpressFear.001` | &lt;fear&gt;I don't know who to trust.&lt;/fear&gt; | {"weight": 1} |
| `starter.ExpressFear.002` | I'm dreading tonight. | {"weight": 1} |
| `starter.ExpressFear.003` | What if we get this wrong? | {"weight": 1} |
| `starter.ExpressFear.004` | I wish I felt safer here. | {"weight": 1} |

### LastWords (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.LastWords.001` | &lt;emphasis&gt;Remember who pushed for this.&lt;/emphasis&gt; | {"weight": 1} |
| `starter.LastWords.002` | I've said all I can. | {"weight": 1} |
| `starter.LastWords.003` | Be careful with your next choice. | {"weight": 1} |
| `starter.LastWords.004` | I hope this helps you see things clearly. | {"weight": 1} |

### ReactToDeath (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.ReactToDeath.001` | I was hoping we'd all make it. | {"weight": 1} |
| `starter.ReactToDeath.002` | We can't let this keep happening. | {"weight": 1} |
| `starter.ReactToDeath.003` | It's hard to talk with someone missing. | {"weight": 1} |
| `starter.ReactToDeath.004` | I keep expecting them to speak. | {"weight": 1} |
| `starter.social.death.observed` | There's an empty place here today. We need to get this right. | {"conditions": [{"kind": "observedDeath"}, {"kind": "phase", "value": "Discussion"}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

### ReactToRoleReveal (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.ReactToRoleReveal.001` | That changes things. | {"weight": 1} |
| `starter.ReactToRoleReveal.002` | We should go back over what they told us. | {"weight": 1} |
| `starter.ReactToRoleReveal.003` | Think about who believed their claim. | {"weight": 1} |
| `starter.ReactToRoleReveal.004` | Now we know a little more. | {"weight": 1} |

### ReportAction (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `report-action.1` | {result} | {"weight": 1} |
| `report-action.2` | About last night: {result} | {"weight": 1} |
| `report-action.3` | Here's what happened: {result} | {"weight": 1} |
| `starter.ReportAction.004` | My report: {result} | {"weight": 1} |

### ReportInvestigation (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.ReportInvestigation.001` | &lt;evidence&gt;I checked {target} last night. The result was {result}.&lt;/evidence&gt; | {"weight": 1} |
| `starter.ReportInvestigation.002` | My check on {target} came back {result}. | {"weight": 1} |
| `starter.ReportInvestigation.003` | I got {result} for {target}. That's what I have. | {"weight": 1} |
| `starter.ReportInvestigation.004` | I looked into {target}. They appeared {result}. | {"weight": 1} |
| `starter.social.report.dawn` | I checked {target} last night. They appeared {result}. | {"conditions": [{"kind": "minimumDay", "number": 2}, {"flag": true, "kind": "hasTarget"}, {"flag": true, "kind": "hasAssertion"}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

### StaySilent (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.StaySilent.001` | (listens) | {"weight": 1} |
| `starter.StaySilent.002` | (stays quiet) | {"weight": 1} |
| `starter.StaySilent.003` | (says nothing) | {"weight": 1} |
| `starter.StaySilent.004` | (waits) | {"weight": 1} |

### VoteJustification (4)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.VoteJustification.001` | I voted for what seemed right. | {"weight": 1} |
| `starter.VoteJustification.002` | It was the best call I could make. | {"weight": 1} |
| `starter.VoteJustification.003` | I know my vote matters. | {"weight": 1} |
| `starter.VoteJustification.004` | I wasn't certain. I had to choose. | {"weight": 1} |

### Vouch (5)

| Stable ID | Exact authored text | Other fields |
| --- | --- | --- |
| `starter.Vouch.001` | I trust {target} for now. | {"weight": 1} |
| `starter.Vouch.002` | I'd give {target} another chance. | {"weight": 1} |
| `starter.Vouch.003` | Let's not rush to condemn {target}. | {"weight": 1} |
| `starter.Vouch.004` | I'm willing to back {target}. | {"weight": 1} |
| `starter.social.vouch.claim` | Let's give {target} a chance to back that up. | {"conditions": [{"kind": "speakerTrait", "value": "Loyal"}, {"kind": "targetHasClaim"}], "metadata": {"emotionKey": "tense"}, "weight": 3} |

## Narration, result and interface templates

Named tokens are substituted from authorized game state. They can be reordered in the source.

### button (14)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `button.accuse` | ACCUSE | — |
| `button.back` | Back | — |
| `button.chat` | CHAT | — |
| `button.claim` | CLAIM ROLE | — |
| `button.continue` | Continue [Space] | — |
| `button.counterAccuse` | Counter-accuse | — |
| `button.defend` | DEFEND | — |
| `button.defendResponse` | Defend yourself | — |
| `button.drawer` | ≡ | — |
| `button.everyone` | Everyone | — |
| `button.newRun` | New run | — |
| `button.pass` | Pass | — |
| `button.quit` | Quit | — |
| `button.self` | Yourself | — |

### choice (65)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `choice.abstain` | Abstain | — |
| `choice.abstainHint` | Cast no target vote. | — |
| `choice.abstainTrial` | Abstain | — |
| `choice.abstainTrialHint` | A majority starts a trial. | — |
| `choice.accuse` | Accuse {target} | target |
| `choice.accuseHint` | Raise suspicion. | — |
| `choice.agree` | Agree with {target} | target |
| `choice.agreeHint` | Support their view. | — |
| `choice.awaitVerdict` | Await the verdict | — |
| `choice.awaitVerdictHint` | You cannot vote in your own trial. | — |
| `choice.beginDiscussion` | Begin discussion | — |
| `choice.beginDiscussionHint` | Start the day. | — |
| `choice.bluffHint` | Bluff. Contradictions may expose you. | — |
| `choice.claim` | Claim {role} | role |
| `choice.continue` | Continue | — |
| `choice.continueTallyHint` | Acknowledge the public tally. | — |
| `choice.counterAccuse` | Counter-accuse | — |
| `choice.counterAccuseHint` | Challenge your accuser. | — |
| `choice.defend` | Defend yourself | — |
| `choice.defendHint` | Push back on suspicion. | — |
| `choice.defendResponse` | Defend yourself | — |
| `choice.defendResponseHint` | Answer the accusation. | — |
| `choice.deflect` | Deflect toward {target} | target |
| `choice.deflectHint` | Shift attention away from you. | — |
| `choice.disagree` | Disagree with {target} | target |
| `choice.disagreeHint` | Challenge their view. | — |
| `choice.errorOption` | That option is no longer available. | — |
| `choice.errorPhrasing` | That phrasing is no longer available. | — |
| `choice.fear` | Express fear | — |
| `choice.fearHint` | Share your unease. | — |
| `choice.judge` | Vote | — |
| `choice.judgeHint` | Hear the verdict. | — |
| `choice.lastWords` | Speak last words | — |
| `choice.lastWordsContinue` | Continue | — |
| `choice.lastWordsHint` | Execution follows. | — |
| `choice.nightPass` | Pass tonight | — |
| `choice.nightPassHint` | No visit. Keeps your charges. | — |
| `choice.nightTarget` | {ability}: {target} | ability, target |
| `choice.nightTargetHint` | Private night action. | — |
| `choice.nominate` | Nominate {target} | target |
| `choice.nominateHint` | Public nomination. | — |
| `choice.reportAction` | Report last night's action | — |
| `choice.reportActionHint` | Share your action and results. | — |
| `choice.reportBluff` | Report {target} as {result} | result, target |
| `choice.reportBluffHint` | State a finding. You may bluff. | — |
| `choice.reportHonest` | Report {target} as {result} | result, target |
| `choice.reportHonestHint` | Share your actual readout. | — |
| `choice.silent` | Stay silent | — |
| `choice.silentHint` | Listen this turn. | — |
| `choice.silentResponse` | Stay silent | — |
| `choice.silentResponseHint` | Decline to answer. | — |
| `choice.trialDefend` | Defend yourself | — |
| `choice.trialDefendHint` | Make your case. | — |
| `choice.trialTarget` | Put {target} on trial | target |
| `choice.trialTargetHint` | Ask for a trial. | — |
| `choice.truthHint` | Tell the truth. | — |
| `choice.verdictHint` | Guilty must exceed Innocent. | — |
| `choice.voteTarget` | Vote {target} | target |
| `choice.voteTargetHint` | The largest vote total determines elimination. | — |
| `choice.vouch` | Vouch for {target} | target |
| `choice.vouchHint` | Offer your support. | — |
| `choice.watch` | Watch | — |
| `choice.watchHint` | Continue the run. | — |
| `choice.yieldOpening` | Let someone else open | — |
| `choice.yieldOpeningHint` | Listen first. Keeps your turn. | — |

### death (5)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `death.executed` | {name} was executed. | name |
| `death.night` | {name} died during the night. | name |
| `death.none` | Nobody died during the night. | — |
| `death.reveal` | Revealed: {name} was {role} ({faction}). | faction, name, role |
| `death.spectating` | You died. You can keep watching. | — |

### drawer (18)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `drawer.claims` | Claims ledger | — |
| `drawer.close` | Close drawer | — |
| `drawer.copySeed` | Copy seed | — |
| `drawer.empty` | No entries yet. | — |
| `drawer.help` | Chat, accuse or defend, then select a valid roster target. Choose the words you want to say.<br><br>First Space/click reveals the line; the next advances. Pass leaves the floor. Back keeps your turn.<br><br>Claims are assertions, not proven roles. Your role and private results are only yours.<br><br>Settings change display immediately and game rules on the next run. | — |
| `drawer.helpLabel` | Help | — |
| `drawer.history` | History | — |
| `drawer.intro` | History, claims and private role information. | — |
| `drawer.ledgerEntry` | Day {day} · {line} | day, line |
| `drawer.newRun` | New run | — |
| `drawer.noReview` | Available after the run ends. | — |
| `drawer.noRole` | Start a run to receive a role. | — |
| `drawer.review` | After-action report | — |
| `drawer.role` | Your role | — |
| `drawer.roleInfo` | {role} · {faction}<br><br>{objective}<br><br>Remaining uses: {uses} | faction, objective, role, uses |
| `drawer.seedCopied` | Seed copied: {seed} | seed |
| `drawer.settings` | Settings | — |
| `drawer.unlimited` | Unlimited | — |

### error (19)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `error.ballot` | Choose a valid ballot target. | — |
| `error.dawn` | Acknowledge dawn. | — |
| `error.defense` | Choose a defense or role claim. | — |
| `error.discussion` | Invalid discussion statement. | — |
| `error.hearDefense` | Acknowledge the defense. | — |
| `error.hearLastWords` | Acknowledge last words. | — |
| `error.lastWords` | Choose last words. | — |
| `error.night` | Invalid night target or exhausted ability. | — |
| `error.noAction` | There is no previous night action. | — |
| `error.noInput` | No active input request. | — |
| `error.nominee` | Invalid nominee. | — |
| `error.phase` | This phase does not accept participant input. | — |
| `error.phrasing` | That phrasing is no longer available. | — |
| `error.resolving` | A turn is already resolving. | — |
| `error.response` | Respond to the public accusation first. | — |
| `error.selfJudge` | Defendants cannot judge themselves. | — |
| `error.spectator` | Dead participants may only spectate. | — |
| `error.tally` | Acknowledge the public ballot result. | — |
| `error.verdict` | Choose a verdict. | — |

### faction (3)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `faction.Antagonist` | Antagonist | — |
| `faction.Neutral` | Neutral | — |
| `faction.Town` | Town | — |

### interaction (4)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `interaction.action.Accuse` | Accuse | — |
| `interaction.action.Chat` | Chat | — |
| `interaction.action.Claim` | Claim | — |
| `interaction.action.Defend` | Defend | — |

### label (20)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `label.ability.Attack` | Attack | — |
| `label.ability.Block` | Block | — |
| `label.ability.Forge` | Forge evidence | — |
| `label.ability.Investigate` | Investigate | — |
| `label.ability.None` | No night ability | — |
| `label.ability.Protect` | Protect | — |
| `label.ability.SelfProtect` | Self-protect | — |
| `label.ability.Watch` | Watch visits | — |
| `label.phase.Dawn` | Morning | — |
| `label.phase.Defense` | Defense | — |
| `label.phase.Discussion` | Discussion | — |
| `label.phase.Ended` | Run complete | — |
| `label.phase.Execution` | Execution | — |
| `label.phase.Judgement` | Judgement | — |
| `label.phase.LastWords` | Last words | — |
| `label.phase.NightResolution` | Night | — |
| `label.phase.NightSelection` | Night | — |
| `label.phase.Nomination` | Nominations | — |
| `label.phase.VoteReveal` | Vote reveal | — |
| `label.phase.Voting` | Vote | — |

### mark (3)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `mark.Confirmed` | Confirmed | — |
| `mark.Suspect` | Suspect | — |
| `mark.Trust` | Trust | — |

### night (20)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `night.claimed.Attack` | I went after someone last night. | — |
| `night.claimed.Block` | I tried to stop someone last night. | — |
| `night.claimed.Forge` | I planted evidence last night. | — |
| `night.claimed.Investigate` | I checked someone last night. | — |
| `night.claimed.None` | I had no night action. | — |
| `night.claimed.Protect` | I tried to keep someone safe last night. | — |
| `night.claimed.SelfProtect` | I tried to keep myself safe last night. | — |
| `night.claimed.Watch` | I kept watch last night. | — |
| `night.selection.Attack` | I tried to attack {target} last night. | target |
| `night.selection.Block` | I tried to block {target} last night. | target |
| `night.selection.Forge` | I tried to plant evidence against {target} last night. | target |
| `night.selection.Investigate` | I tried to check on {target} last night. | target |
| `night.selection.None` | I had no night action. | — |
| `night.selection.Pass` | I stayed in last night. | — |
| `night.selection.Protect` | I tried to protect {target} last night. | target |
| `night.selection.SelfProtect` | I tried to keep myself safe last night. | — |
| `night.selection.Watch` | I tried to watch {target} last night. | target |
| `night.team` | Team: {name} will {action}. | action, name |
| `night.teamAttack` | attack {target} | target |
| `night.teamPass` | pass tonight | — |

### objective (7)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `objective.Antagonist` | Help your team take control. | — |
| `objective.NeutralBenign` | Survive to the end. | — |
| `objective.NeutralEvil` | Be eliminated by the daytime vote. | — |
| `objective.NeutralKilling` | Eliminate the competing factions. | — |
| `objective.Town` | Eliminate the hostile factions. | — |
| `objective.achieved` | achieved | — |
| `objective.missed` | missed | — |

### outcome (4)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `outcome.AntagonistVictory` | Antagonists win. | — |
| `outcome.Draw` | Draw. | — |
| `outcome.LoneKillerVictory` | The Lone Killer wins. | — |
| `outcome.TownVictory` | Town wins. | — |

### phase (9)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `phase.dawn` | Day {day}. | day |
| `phase.discussion` | Discussion begins. | — |
| `phase.judgement` | Time to vote. | — |
| `phase.lastWords` | Last words. | — |
| `phase.night` | Night {day}. | day |
| `phase.nomination` | Nominations open. | — |
| `phase.revote` | Revote. | — |
| `phase.trial` | Trial {trial}/{maximum}: {name}. | maximum, name, trial |
| `phase.voting` | Vote. | — |

### report (9)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `report.attackFailed` | My attack failed. | — |
| `report.blocked` | Someone blocked me. I don't know who. | — |
| `report.forged` | I made the target look like an Antagonist for the night. | — |
| `report.investigation` | {target} appeared {result}. | result, target |
| `report.noVisitors` | I saw no other visitors to {target}. | target |
| `report.private` | Private: {report} | report |
| `report.protection` | My protection took effect; I can't tell if anyone attacked. | — |
| `report.survived` | I survived an attack. I don't know who it was. | — |
| `report.visit` | {visitor} visited {target}. | target, visitor |

### review (3)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `review.night` | Night {night}: {name} ({role}) {action} | action, name, night, role |
| `review.role` | {name}: {role} ({faction}) | faction, name, role |
| `review.vote` | Day {day}: {name} voted {target}; their suspicion was {suspicion}/100 and credibility {credibility}/100. | credibility, day, name, suspicion, target |

### roster (9)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `roster.alive` | ALIVE | — |
| `roster.card` | {name}{self}<br>{state}{activity}{mark}{claim} | activity, claim, mark, name, self, state |
| `roster.claim` | <br>Claims {role} | role |
| `roster.dead` | DEAD | — |
| `roster.mark` | <br>Mark: {mark} | mark |
| `roster.select` |  · SELECT | — |
| `roster.self` |  (you) | — |
| `roster.speaking` |  · SPEAKING | — |
| `roster.teammate` | TEAMMATE | — |

### run (3)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `run.cast` | Starting cast: {town} Town · {evil} Antagonist · {neutral} Neutral. | evil, neutral, town |
| `run.ended` | Run ended: {result} Objective {objective}. | objective, result |
| `run.identity` | You are {name}, the {role} ({faction}). | faction, name, role |

### screen (15)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `screen.actionHeader` | DAY {day} · {phase}{round} · ALIVE {alive} | alive, day, phase, round |
| `screen.advanceHint` | Space/click to continue | — |
| `screen.chooseTarget` | {action} · choose a highlighted roster target | action |
| `screen.discussionRound` | DISCUSSION · ROUND {round}/{maximum} | maximum, round |
| `screen.header` | DAY {day} · {phase} | day, phase |
| `screen.phase.elimination` | ELIMINATION | — |
| `screen.phase.ended` | GAME OVER | — |
| `screen.phase.morning` | MORNING REPORT | — |
| `screen.phase.night` | NIGHT | — |
| `screen.phase.report` | REPORT | — |
| `screen.phase.voteReveal` | VOTE REVEAL | — |
| `screen.phrasing` | [{tone}] {line} | line, tone |
| `screen.respond` | Respond to {name} | name |
| `screen.revealHint` | Space/click reveals this line | — |
| `screen.roundSuffix` |  · ROUND {round}/{maximum} | maximum, round |

### separator (2)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `separator.names` | ,  | — |
| `separator.tally` |  ·  | — |

### settings (36)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `settings.close` | Close settings | — |
| `settings.group.Accessibility` | Accessibility | — |
| `settings.group.Data` | Data | — |
| `settings.group.Game` | Game | — |
| `settings.group.Text & Display` | Text &amp; Display | — |
| `settings.label.abstain` | Allow abstentions | — |
| `settings.label.auto` | Auto advance | — |
| `settings.label.colorblind` | Neutral accent palette | — |
| `settings.label.count` | Participants | — |
| `settings.label.delay` | Auto advance hold | — |
| `settings.label.effects` | Effects | — |
| `settings.label.flashes` | Allow safe flashes | — |
| `settings.label.instant` | Instant text | — |
| `settings.label.intensity` | Effects intensity | — |
| `settings.label.lastWords` | Last words | — |
| `settings.label.motion` | Reduced motion | — |
| `settings.label.quick` | Quick phrasing | — |
| `settings.label.reveal` | Reveal roles on death | — |
| `settings.label.revote` | One tie revote | — |
| `settings.label.rounds` | Discussion rounds | — |
| `settings.label.scale` | UI scale | — |
| `settings.label.size` | Text size | — |
| `settings.label.speed` | Text speed | — |
| `settings.label.trial` | Trial Mode | — |
| `settings.label.trials` | Trials per day (Trial Mode) | — |
| `settings.label.windows` | Player windows | — |
| `settings.off` | Off | — |
| `settings.on` | On | — |
| `settings.policy` | Display changes apply now. Game changes apply to the next run. | — |
| `settings.reset` | Reset | — |
| `settings.resetAll` | Reset all defaults | — |
| `settings.saveError` | Preferences could not be kept: {reason} | reason |
| `settings.seedError` | Seed must be a number from 0 to 4294967295. | — |
| `settings.seedHint` | Seed (blank = random) | — |
| `settings.toggle` | {title}: {value} | title, value |
| `settings.value` | {title}: {value} | title, value |

### setup (9)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `setup.chooseRole` | Choose your role | — |
| `setup.error` | Invalid setup: {reason} | reason |
| `setup.faction` | {faction} roles | faction |
| `setup.header` | SOCIALSTIM · NEW RUN | — |
| `setup.intro` | Configure the next run in ≡ → Settings. Each run starts fresh. | — |
| `setup.random` | Random role | — |
| `setup.randomSeed` | Random seed | — |
| `setup.seed` | Seed {seed} | seed |
| `setup.summary` | {count} participants · {seed} | count, seed |

### speech (1)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `speech.attributed` | {name}: {line} | line, name |

### tone (8)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `tone.challenge` | Challenge | — |
| `tone.concern` | Concern | — |
| `tone.conversation` | Conversation | — |
| `tone.counter` | Counter | — |
| `tone.result` | Result | — |
| `tone.role` | Role | — |
| `tone.selfDefense` | Self-defense | — |
| `tone.support` | Support | — |

### trial (6)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `trial.ballot` | {name}: {verdict}. | name, verdict |
| `trial.guilty` | {name} is found guilty. | name |
| `trial.innocent` | {name} is acquitted. | name |
| `trial.noMajority` | No majority. No trial today. | — |
| `trial.nobody` | nobody | — |
| `trial.nomination` | {name} nominates {target}. | name, target |

### verdict (3)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `verdict.Abstain` | Abstain | — |
| `verdict.Guilty` | Guilty | — |
| `verdict.Innocent` | Innocent | — |

### vote (8)

| Stable ID | Exact authored template | Tokens |
| --- | --- | --- |
| `vote.abstainTarget` | to abstain | — |
| `vote.allAbstained` | Everyone abstained. | — |
| `vote.ballot` | {name} votes {target}. | name, target |
| `vote.count` | {name}: {count} | count, name |
| `vote.empty` | No votes cast | — |
| `vote.revoteBetween` | Revote between {names}. | names |
| `vote.tally` | {tally} · Abstentions: {count}. | count, tally |
| `vote.tied` | The vote is tied. | — |
