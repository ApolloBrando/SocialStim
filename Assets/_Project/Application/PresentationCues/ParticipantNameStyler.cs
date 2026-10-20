using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Highlights public participant names once per displayed line, preserving literal text and existing markup.</summary>
    public static class ParticipantNameStyler
    {
        public static SemanticText Highlight(SemanticText text, IEnumerable<string> names)
        {
            var spans = text.Spans.ToList(); var occupied = new bool[text.Text.Length];
            foreach (string name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal)
                .OrderByDescending(n => n.Length).ThenBy(n => n, StringComparer.Ordinal))
            {
                int start = 0;
                while (start < text.Text.Length)
                {
                    int index = text.Text.IndexOf(name, start, StringComparison.Ordinal);
                    if (index < 0) break;
                    int end = index + name.Length; start = end;
                    if (index > 0 && Word(text.Text[index - 1]) || end < text.Text.Length && Word(text.Text[end])) continue;
                    if (Enumerable.Range(index, name.Length).Any(i => occupied[i])) continue;
                    for (int i = index; i < end; i++) occupied[i] = true;
                    spans.Add(new SemanticSpan(index, name.Length, SemanticStyle.Participant));
                }
            }
            return new SemanticText(text.Text, spans);
        }
        /// <summary>Only the attributed leading speaker name uses character identity colour; mentions keep their shared style.</summary>
        public static SemanticText Dialogue(SemanticText text, IEnumerable<string> names, string speaker, int characterIndex)
        {
            SemanticText highlighted = Highlight(text, names);
            if (string.IsNullOrEmpty(speaker) || !text.Text.StartsWith(speaker + ":", StringComparison.Ordinal)) return highlighted;
            var spans = highlighted.Spans.ToList();
            spans.Add(new SemanticSpan(0, speaker.Length, SpeakerStyle(characterIndex)));
            return new SemanticText(text.Text, spans);
        }
        public static SemanticStyle SpeakerStyle(int characterIndex)
        {
            if (characterIndex < 0) throw new ArgumentOutOfRangeException(nameof(characterIndex));
            return (SemanticStyle)((int)SemanticStyle.Speaker0 + characterIndex % 12);
        }
        private static bool Word(char value) => char.IsLetterOrDigit(value) || value == '_' || value == '-';
    }
}
