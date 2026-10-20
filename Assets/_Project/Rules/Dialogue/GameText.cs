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
            ["ui.you"] = "(you)",
            ["ui.dead"] = "DEAD",
            ["ui.alive"] = "ALIVE",
            ["ui.teammate"] = "TEAMMATE",
            ["ui.legend"] = "COLOUR KEY",
            ["ui.legendBody"] = "Speaker names and living player-list names share a character colour. Names mentioned inside sentences use blue. Character colours do not indicate factions. Underlined action labels identify each statement; reply descriptions name the point being supported or challenged. Role badges: Town green, Antagonist red, Neutral orange. A claim is not proof. Dead players are grey.",
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
            ["report.guilt"] = "I killed a Town participant. I cannot shoot again, and I will die from guilt the following night.",
            ["outcome.serialKiller"] = "Serial Killer wins.",
            ["outcome.ego"] = "Ego steals the win.",
            ["ui.jesterRevenge"] = "Jester's final act",
            ["ui.revengeTarget"] = "Take revenge on {name}",
            ["ui.revengeHelp"] = "Choose a living Town participant. This final action resolves before night begins.",
            ["event.jesterVictim"] = "{name} was killed by the Jester's final act.",
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
            ["role.inspector"] = "Alignment: Town\nAction: Investigate one other participant each night and learn their apparent alignment.\nLimits: Forger can alter a reading. Ego appears Town. A result is not a secret-role reveal.\nGoal: Eliminate all Antagonists and the Serial Killer.\nClaim: May truthfully claim Sheriff.",
            ["role.physician"] = "Alignment: Town\nAction: Protect one other participant at night.\nLimits: Basic protection stops basic attacks, but not a Serial Killer. Applying protection does not prove an attack occurred.\nGoal: Eliminate all Antagonists and the Serial Killer.\nClaim: May truthfully claim Physician.",
            ["role.citizen"] = "Alignment: Town\nAction: Use discussion and voting to identify threats.\nLimits: No active night ability.\nGoal: Eliminate all Antagonists and the Serial Killer.\nClaim: Cannot claim a special role or bluff.",
            ["role.vigilante"] = "Alignment: Town\nAction: Shoot one other participant at night, starting on Night 2.\nLimits: Three basic shots. Passing or being blocked preserves a shot. Killing a Town participant removes all remaining shots and causes unavoidable guilt death the following night.\nGoal: Eliminate all Antagonists and the Serial Killer.\nClaim: May truthfully claim Vigilante.",
            ["role.coordinator"] = "Alignment: Town\nAction: Watch another participant and learn who visited them that night.\nLimits: See visitor identities, not abilities or the watched participant's outgoing action. A living counterclaimant is a legal target.\nGoal: Eliminate all Antagonists and the Serial Killer.\nClaim: May truthfully claim Lookout.",
            ["role.assassin"] = "Alignment: Antagonist\nAction: Carry out the faction's basic night attack.\nLimits: One coordinated kill per team per night. Know your teammates. A living player Mafioso controls the target or pass. Cannot attack teammates.\nGoal: Secure an Antagonist victory before Ego can steal it.\nClaim: May bluff a Town special role.",
            ["role.interrupter"] = "Alignment: Antagonist\nAction: Block another participant's night ability.\nLimits: Accepted blocks resolve simultaneously. Blocking does not remove passive immunity or prevent scheduled guilt death. Know your teammates.\nGoal: Secure an Antagonist victory before Ego can steal it.\nClaim: May bluff a Town special role.",
            ["role.forger"] = "Alignment: Antagonist\nAction: Make one participant appear Antagonist to investigations for that night.\nLimits: Changes apparent alignment only; expires the next night and does not change the target's real faction. Know your teammates.\nGoal: Secure an Antagonist victory before Ego can steal it.\nClaim: May bluff a Town special role.",
            ["role.lone-killer"] = "Alignment: Neutral\nAction: Make an independent powerful night attack.\nLimits: Basic innate defense. No Antagonist teammate knowledge. Cannot kill Ego at night; the two alone form a draw.\nGoal: Be the last survivor.\nClaim: May bluff a Town special role.",
            ["role.survivor"] = "Alignment: Neutral\nAction: Use discussion and voting to stay alive; no active night ability.\nLimits: Immune to night attacks, including while blocked. Can be executed by vote. Appears Town to investigations unless forged.\nGoal: Survive until a normal Antagonist victory and replace it. Surviving a Town victory does not win.\nClaim: May bluff a Town special role.",
            ["role.provocateur"] = "Alignment: Neutral\nAction: Use discussion and voting to draw a day execution; no active night ability.\nLimits: A night death does not fulfill the objective. After day execution, choose one living Town participant to kill before night begins.\nGoal: Be executed by the day vote.\nClaim: May bluff a Town special role.",
        };
    }
}
