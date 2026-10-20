using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Portable reference writer shared by Unity authoring tools; matches the repository's Python freshness check.</summary>
    public static class DialogueCatalogue
    {
        /// <summary>Generates every current entry from source without making displayed text authoritative in code.</summary>
        public static string Render(string speechJson, string copyJson)
        {
            var speech = new EditableTextDocument(speechJson); var copy = new EditableTextDocument(copyJson);
            speech.Validate(); copy.Validate();
            if (speech.IsCopy || !copy.IsCopy) throw new ArgumentException("Expected spoken dialogue followed by copy templates.");
            var lines = new List<string> {
                "# Complete dialogue and game-text catalogue", "",
                "Generated from the editable JSON files. Edit the source, then run",
                "`python3 Tools/dialogue_catalogue.py`. CI checks this document stays synchronized.", "",
                "Current content: **" + speech.Ids.Count + " spoken lines** and **" + copy.Ids.Count + " narration/interface templates**.",
                "Templates are not additional conversational variants and do not count toward the 500 player-line target.", "",
                "| Edit | Source |", "| --- | --- |",
                "| Spoken lines shared by players and NPCs | [StarterDialogue.json](../Assets/_Project/Data/Dialogue/StarterDialogue.json) |",
                "| Narration, night results, choice labels, menus and help | [SocialStimText.json](../Assets/_Project/Data/Dialogue/Resources/SocialStimText.json) |",
                "| Editing instructions | [DIALOGUE_GUIDE.md](DIALOGUE_GUIDE.md) |", "",
                "## Spoken lines", "",
                "Eligibility still depends on the current role, phase and command. A line listed here is not necessarily available now.", ""
            };
            foreach (var group in speech.Ids.GroupBy(speech.Group).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                lines.AddRange(new[] { "### " + group.Key + " (" + group.Count() + ")", "", "| Stable ID | Exact authored text | Other fields |", "| --- | --- | --- |" });
                foreach (string id in group.OrderBy(id => id, StringComparer.Ordinal))
                    lines.Add("| `" + id + "` | " + Cell(speech.Text(id)) + " | " + Cell(speech.OtherFields(id)) + " |");
                lines.Add("");
            }
            lines.AddRange(new[] { "## Narration, result and interface templates", "", "Named tokens are substituted from authorized game state. They can be reordered in the source.", "" });
            var tokenPattern = new Regex(@"\{([A-Za-z][A-Za-z0-9]*)\}");
            foreach (var group in copy.Ids.GroupBy(copy.Group).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                lines.AddRange(new[] { "### " + group.Key + " (" + group.Count() + ")", "", "| Stable ID | Exact authored template | Tokens |", "| --- | --- | --- |" });
                foreach (string id in group.OrderBy(id => id, StringComparer.Ordinal))
                {
                    var tokens = tokenPattern.Matches(copy.Text(id)).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToArray();
                    lines.Add("| `" + id + "` | " + Cell(copy.Text(id)) + " | " + (tokens.Length == 0 ? "—" : string.Join(", ", tokens)) + " |");
                }
                lines.Add("");
            }
            return string.Join("\n", lines);
        }
        private static string Cell(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("|", "&#124;").Replace("\n", "<br>");
    }
}
