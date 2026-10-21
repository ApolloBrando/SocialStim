using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Immutable authored interface/report wording. The Unity catalog loads the editable JSON.</summary>
    public sealed class GameText
    {
        private readonly IReadOnlyDictionary<string, string> _entries;
        /// <summary>Compatibility defaults for clients without a Unity content catalog.</summary>
        public static GameText Default { get; } = new GameText(DefaultEntries(), false);
        /// <summary>Copies and validates an authored text catalog.</summary>
        public GameText(IEnumerable<KeyValuePair<string, string>> entries) : this(entries, true) { }
        private GameText(IEnumerable<KeyValuePair<string, string>> entries, bool validate)
        {
            var values = entries?.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)
                ?? throw new ArgumentNullException(nameof(entries));
            if (values.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value))
                || validate && Default._entries.Keys.Any(key => !values.ContainsKey(key)))
                throw new ArgumentException("The text catalog has missing or empty entries.");
            _entries = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(values);
            if (validate)
                foreach (var pair in Default._entries)
                {
                    var expected = Regex.Matches(pair.Value, @"\{[A-Za-z]+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s);
                    var actual = Regex.Matches(values[pair.Key], @"\{[A-Za-z]+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s);
                    if (!expected.SequenceEqual(actual)) throw new ArgumentException("Incorrect template fields for " + pair.Key);
                }
        }
        /// <summary>Detached editable entries, also usable by portable authoring checks.</summary>
        public IReadOnlyDictionary<string, string> Entries => _entries;
        /// <summary>Formats explicit named fields; prose is never generated from an NPC strategy.</summary>
        public string Format(string key, params (string name, string value)[] values)
        {
            if (!_entries.TryGetValue(key, out string result)) throw new ArgumentException("Unknown text key: " + key);
            foreach (var value in values) result = result.Replace("{" + value.name + "}", value.value ?? "");
            return result;
        }
        private static IEnumerable<KeyValuePair<string, string>> DefaultEntries() => new Dictionary<string, string> {
            ["fabricate.selected"] = "Selected visitors: {names}",
            ["fabricate.none"] = "None",
            ["mafia.solo"] = "Private Mafia update: you have no teammates this game.",
            ["mafia.members"] = "Private Mafia teammates: {members}.",
            ["mafia.member"] = "{name} ({role})",
            ["mafia.action"] = "Private Mafia action · {name}: {report}",
            ["mafia.promotion"] = "Private Mafia update: {name} has become the {role} and now controls a Mafia kill.",
            ["mafia.log"] = "Mafia log (private)",
            ["mafia.teamRole"] = "Team: {role}",
            ["mafia.noReports"] = "Night actions will appear here after the first night.",
            ["fabricate.start"] = "Fabricate report",
            ["fabricate.help"] = "Choose a cover story for your claimed role. Only publishing spends your turn.",
            ["fabricate.back"] = "Back",
            ["fabricate.pickTarget"] = "{role} · Night {night}\nChoose your reported target from the player list.",
            ["fabricate.pickResult"] = "{role} · Night {night} · {target}\nChoose the result you want to report.",
            ["fabricate.pickVisitors"] = "{role} · Night {night} · {target}\nToggle visitors in the player list, then publish.",
            ["fabricate.pass"] = "Report that I passed",
            ["fabricate.blocked"] = "Report that I was blocked",
            ["fabricate.alignment"] = "Report {result}",
            ["fabricate.protected"] = "Report protection applied",
            ["fabricate.attack"] = "Report the shot with no confirmed result",
            ["fabricate.failed"] = "Report that the attack failed",
            ["fabricate.noVisitors"] = "Report no visitors",
            ["fabricate.visitors"] = "Choose the visitors I claim to have seen",
            ["fabricate.publishVisitors"] = "Publish report · {count} visitors",
            ["fabricate.addVisitor"] = "Add {name} as a visitor",
            ["fabricate.removeVisitor"] = "Remove {name} from visitors",
            ["faction.Town"] = "Town",
            ["faction.Antagonist"] = "Mafia",
            ["faction.Neutral"] = "Neutral",
            ["ui.revengeTarget"] = "Haunt {name}",
            ["ui.revengePrompt"] = "Jester revenge — choose a living Town member who voted guilty.",
            ["event.revenge"] = "{name} was taken by the executed Jester before nightfall.",
            ["event.guiltQueued"] = "You killed a Town member. Guilt prevents another shot and will kill you at the end of the next night.",
            ["event.serialKillerWins"] = "Serial Killer wins.",
            ["event.egoWins"] = "Ego steals the win.",
            ["ui.relations"] = "Relation debug",
            ["ui.relationsTitle"] = "NPC RELATIONS — privileged opinions, not facts",
            ["ui.relationsEmpty"] = "Start a run to inspect NPC opinions.",
            ["ui.relationsTrust"] = "Trust: {value} / 10",
            ["ui.relationsSuspicion"] = "Suspicion: {value} / 10",
            ["ui.relationsHelp"] = "Read from the centre out: the centre NPC is judging each person an arrow points to. Longer Trust bars mean more credible; longer Suspicion bars mean more suspected. Both can be high together. Click an NPC card to switch viewpoint. Page through targets below. These are opinions, not confirmed roles.",
            ["ui.relationsObserver"] = "Viewpoint: {name}",
            ["ui.relationsScores"] = "Suspicion: {suspicion}/10\nTrust: {trust}/10",
            ["ui.relationsCentre"] = "VIEWPOINT\n{name}",
            ["ui.relationsEstimates"] = "Town {town}% · Mafia {mafia}% · Neutral {neutral}%",
            ["ui.previousNpc"] = "Previous NPC",
            ["ui.nextNpc"] = "Next NPC",
            ["ui.previousTargets"] = "Previous targets",
            ["ui.nextTargets"] = "Next targets",
            ["ui.relationsPage"] = "Targets {page}/{pages}",
            ["role.short.citizen"] = "VIL",
            ["role.short.inspector"] = "SHF",
            ["role.short.physician"] = "PHY",
            ["role.short.vigilante"] = "VIG",
            ["role.short.coordinator"] = "LO",
            ["role.short.assassin"] = "MAF",
            ["role.short.interrupter"] = "STR",
            ["role.short.forger"] = "FOR",
            ["role.short.lone-killer"] = "SK",
            ["role.short.survivor"] = "EGO",
            ["role.short.provocateur"] = "JES",
            ["ui.you"] = "(you)",
            ["ui.dead"] = "DEAD",
            ["ui.alive"] = "ALIVE",
            ["ui.teammate"] = "TEAMMATE",
            ["ui.legend"] = "COLOUR KEY",
            ["ui.legendBody"] = "Speaker names and living player-list names share a character colour. Names mentioned inside sentences use blue. Character colours do not indicate factions. Underlined action labels identify each statement; reply descriptions name the point being supported or challenged. Role badges: Town green, Antagonist red, Neutral orange. A claim is not proof. Your first claim or counterclaim locks your claimed role for this run. Dead players are grey.",
            ["intent.Accuse"] = "ACCUSE",
            ["intent.Defend"] = "DEFEND",
            ["intent.Vouch"] = "DEFEND",
            ["intent.Deflect"] = "REFUTE",
            ["intent.Agree"] = "AGREE",
            ["intent.Disagree"] = "DISAGREE",
            ["intent.ClaimRole"] = "CLAIM",
            ["intent.CounterClaim"] = "COUNTERCLAIM",
            ["intent.ReportAction"] = "REPORT",
            ["intent.ReportInvestigation"] = "REPORT",
            ["intent.CallForTrial"] = "ACCUSE",
            ["intent.ExpressFear"] = "CONCERN",
            ["intent.LastWords"] = "LAST WORDS",
            ["ui.statement"] = "STATEMENT",
            ["ui.actionFirst"] = "Choose an action, then a player",
            ["ui.pickForAction"] = "{action} — choose a player [1–9]",
            ["ui.rolesShortcut"] = "Roles [F1]",
            ["ui.latest"] = "Latest dialogue",
            ["ui.textSpeed"] = "Text speed: {percent}%",
            ["ui.speedHelp"] = "0% pauses reveal · arrows adjust · 100% normal",
            ["ui.agreeContext"] = "Support this statement: {statement}",
            ["ui.disagreeContext"] = "Challenge this statement: {statement}",
            ["ui.contextAccuse"] = "{speaker} accused {target}.",
            ["ui.contextDefense"] = "{speaker} defended {target}.",
            ["ui.contextSelfDefense"] = "{speaker} defended themselves.",
            ["ui.contextRefute"] = "{speaker} refuted an accusation and accused {target}.",
            ["ui.giveReport"] = "Give report", ["ui.reportPending"] = "No night to report yet",
            ["ui.reportDone"] = "Report already given", ["ui.selectTarget"] = "Choose a player from the list",
            ["ui.targetActions"] = "Actions for {name}", ["ui.history"] = "Public record",
            ["ui.historyEmpty"] = "No public statements or votes recorded yet.", ["ui.back"] = "Back",
            ["ui.roles"] = "Roles", ["ui.roleGuide"] = "ROLE REFERENCE", ["ui.close"] = "Close",
            ["ui.debug"] = "Night debug", ["ui.debugTitle"] = "NIGHT DEBUG — reveals secret actions",
            ["ui.debugEmpty"] = "No night has resolved yet.", ["ui.debugRow"] = "Night {night}: {name} — {action} — {target}\n{details}",
            ["ui.pass"] = "Pass", ["ui.visited"] = "Visit completed", ["ui.noVisit"] = "No completed visit",
            ["ui.blocked"] = "Blocked", ["ui.spent"] = "Charge spent", ["ui.notSpent"] = "No charge spent",
            ["ui.morning"] = "Morning", ["ui.afternoon"] = "Afternoon", ["ui.evening"] = "Evening", ["ui.night"] = "Night",
            ["ui.accuseTarget"] = "Accuse {name}", ["ui.defendTarget"] = "Defend {name}",
            ["ui.mark"] = "Mark as a suspect", ["ui.unmark"] = "Clear my suspect mark", ["ui.myMark"] = "YOUR SUSPECT",
            ["ui.confirm"] = "Confirm: {action}", ["ui.inspect"] = "SELECT AN ACTION OR PLAYER", ["ui.recordDay"] = "Day {day}",
            ["ui.selectable"] = "SELECTABLE", ["ui.selected"] = "SELECTED", ["ui.previous"] = "Previous page", ["ui.next"] = "Next page",
            ["report.heading"] = "Night {night} — {role} report.", ["report.pass"] = "I chose not to use my ability.",
            ["report.none"] = "I had no active night ability.", ["report.investigate"] = "I chose to investigate {target}.",
            ["report.protect"] = "I chose to protect {target}.", ["report.block"] = "I chose to block {target}.",
            ["report.attack"] = "I chose to attack {target}.", ["report.selfProtect"] = "I chose to protect myself.",
            ["report.forge"] = "I chose to forge evidence against {target}.", ["report.watch"] = "I chose to watch {target}.",
            ["report.blocked"] = "I was blocked; I could not complete my action.",
            ["report.blockedBlock"] = "I was blocked too, but my own selected block still took effect.",
            ["report.investigation"] = "The investigation result for {target} was {result}.",
            ["report.visitor"] = "I saw {visitor} visit {subject}. I could not see which ability they used.",
            ["report.noVisitors"] = "I saw no other visitors to {target}.",
            ["report.protection"] = "My protection was applied to {target}. This does not tell me whether anyone attacked them.",
            ["report.attackFailed"] = "My attack did not kill {target}.",
            ["report.targeted"] = "I survived a night attack. I do not know who attacked me.",
            ["report.forgery"] = "I made {target} appear Antagonist to investigations for that night.",
            ["report.noResult"] = "I received no further result confirming what happened to the target.",
            ["role.inspector"] = "TOWN — Sheriff\nAbility: Investigate one other player each night; learn their apparent faction.\nLimits: Forged evidence can change the result.\nClaims: Claim your own role and report your findings.\nGoal: Help Town eliminate the Mafia and Serial Killer.",
            ["role.physician"] = "TOWN — Physician\nAbility: Protect one other player each night.\nLimits: Basic protection stops basic attacks, but not a powerful Serial Killer attack. A protection notice does not prove an attack happened.\nClaims: Claim your own role and report your protection.\nGoal: Help Town eliminate the Mafia and Serial Killer.",
            ["role.citizen"] = "TOWN — Villager\nAbility: Use discussion and votes; you have no night action.\nLimits: You cannot claim an acting role.\nClaims: No role claim or night report.\nGoal: Help Town eliminate the Mafia and Serial Killer.",
            ["role.vigilante"] = "TOWN — Vigilante\nAbility: Shoot one other player at night with a basic attack.\nLimits: Two bullets. Passing or being blocked preserves a bullet. If your shot kills Town, you cannot shoot again and die from guilt at the end of the following night. Protection and blocking cannot stop guilt.\nClaims: Claim your own role and report your action.\nGoal: Help Town eliminate the Mafia and Serial Killer.",
            ["role.coordinator"] = "TOWN — Lookout\nAbility: Watch one other player each night; learn who visited them.\nLimits: You learn identities, not abilities or the watched player's outgoing target.\nClaims: Claim your own role and report visitors.\nGoal: Help Town eliminate the Mafia and Serial Killer.",
            ["role.assassin"] = "MAFIA — Mafioso\nAbility: Carry out the Mafia's shared basic night attack.\nLimits: One team kill each night. You know your teammates and cannot attack them. A living player Mafioso chooses the team target or pass. If the last Mafioso dies, one surviving Mafia member inherits the role.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Secure a Mafia victory.",
            ["role.interrupter"] = "MAFIA — Strongarm\nAbility: Block one other player's night action.\nLimits: Blocks resolve simultaneously; a blocked Strongarm still carries out its accepted block. You know your teammates and their recorded night actions. You may inherit Mafioso when the team loses its killer.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Secure a Mafia victory.",
            ["role.forger"] = "MAFIA — Forger\nAbility: Forge evidence against one other player each night.\nLimits: The target appears Mafia to investigations for that night only. You know your teammates and their recorded night actions. You may inherit Mafioso when the team loses its killer.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Secure a Mafia victory.",
            ["role.lone-killer"] = "NEUTRAL — Serial Killer\nAbility: Attack one other player each night with a powerful attack.\nLimits: Basic innate defense stops basic attacks. You do not know Mafia teammates. A lone Serial Killer and night-immune Ego form a stalemate.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Outlast Town and the Mafia.",
            ["role.survivor"] = "NEUTRAL — Ego\nAbility: Stay alive; you have no active night ability.\nLimits: Immune to night attacks, but vulnerable to execution. Investigations read Town.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Survive until a normal Mafia win, then steal it. A Town win is your loss.",
            ["role.provocateur"] = "NEUTRAL — Jester\nAbility: Get yourself executed by the day vote.\nLimits: A night death does not count. After execution, choose one living Town member who voted guilty to kill before night. No eligible voter means no revenge kill.\nClaims: Bluff a Town role and give fabricated reports.\nGoal: Be executed; revenge does not change your earned win.",
        };
    }
}
