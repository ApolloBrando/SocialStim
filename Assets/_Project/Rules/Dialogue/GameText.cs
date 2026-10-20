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
            ["role.inspector"] = "TOWN — Decryptor\nInvestigate one other participant each night. You receive their apparent alignment, which can be affected by forged evidence. Goal: help Town eliminate its threats.",
            ["role.physician"] = "TOWN — Physician\nProtect one other participant at night. Protection can stop attacks up to its strength; applying protection does not prove an attack happened. Goal: help Town eliminate its threats.",
            ["role.citizen"] = "TOWN — Solarian\nNo active night ability. Use discussion, reports and votes to find the threats. Cannot claim an acting role. Goal: help Town win.",
            ["role.vigilante"] = "TOWN — Nullifier\nLimited night attacks against one other participant. Passing preserves a charge. Choose carefully: a suspicion is not proof. Goal: help Town win.",
            ["role.coordinator"] = "TOWN — Watcher\nWatch another participant at night. Learn who visited them, not what ability each visitor used. A living counterclaimant is a legal target. Goal: help Town win.",
            ["role.assassin"] = "ANTAGONIST — Archon\nThe faction chooses one night attack. You know your Antagonist teammates. A living player killer controls the team's target or pass. May bluff a Town special role. Goal: secure an Antagonist victory.",
            ["role.interrupter"] = "ANTAGONIST — Singer\nChoose another participant to block at night. A successful block can prevent their action. You know your Antagonist teammates and may bluff a Town special role. Goal: secure an Antagonist victory.",
            ["role.forger"] = "ANTAGONIST — Aperture\nForge evidence against one participant so their investigation appearance becomes Antagonist for that night. You know your teammates and may bluff a Town special role. Goal: secure an Antagonist victory.",
            ["role.lone-killer"] = "NEUTRAL — Anarchy\nMake an independent night attack. You have no Antagonist teammate knowledge. Goal: be the last survivor; Anarchy and the night-immune Infinitum alone form a stalemate.",
            ["role.survivor"] = "NEUTRAL — Infinitum\nNo active night ability; immune to night attacks and appears Town to investigations. Goal: survive until a normal Antagonist victory, which you replace. Surviving a Town victory is not your win.",
            ["role.provocateur"] = "NEUTRAL — Fool\nNo active night ability. Goal: be executed by the day vote. Dying at night does not fulfill your objective. May bluff a Town special role.",
        };
    }
}
