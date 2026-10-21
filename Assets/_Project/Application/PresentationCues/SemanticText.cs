using System.Collections.Generic;
using System.Globalization;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Parsed text and cached text-element boundaries; parsing never runs per frame.</summary>
    public sealed class SemanticText
    {
        private readonly int[] _elements;
        public SemanticText(string text, IEnumerable<SemanticSpan> spans)
        { Text = text; Spans = new List<SemanticSpan>(spans).AsReadOnly(); _elements = StringInfo.ParseCombiningCharacters(text); }
        public string Text { get; }
        public IReadOnlyList<SemanticSpan> Spans { get; }
        public int ElementCount => _elements.Length;
        public int EndOfElements(int count) => count <= 0 ? 0 : count >= _elements.Length ? Text.Length : _elements[count];
        /// <summary>Last nested span wins; no TMP or HTML tags reach the renderer.</summary>
        public SemanticStyle StyleAt(int index)
        {
            SemanticStyle result = SemanticStyle.Default;
            for (int i = 0; i < Spans.Count; i++)
                if (index >= Spans[i].Start && index < Spans[i].Start + Spans[i].Length) result = Spans[i].Style;
            return result;
        }
        public static SemanticText Literal(string text) => new SemanticText(text ?? "", new List<SemanticSpan>());
    }

    /// <summary>Portable semantic emphasis, independent of any render palette.</summary>
    public enum SemanticStyle { Default, Emphasis, Fear, Calm, Evidence, Danger, Participant, ActionPressure, ActionSupport, ActionChallenge, ActionEvidence, Speaker0, Speaker1, Speaker2, Speaker3, Speaker4, Speaker5, Speaker6, Speaker7, Speaker8, Speaker9, Speaker10, Speaker11 }

    /// <summary>A UTF-16 range in the parsed plain text.</summary>
    public readonly struct SemanticSpan
    {
        public SemanticSpan(int start, int length, SemanticStyle style) { Start = start; Length = length; Style = style; }
        public int Start { get; }
        public int Length { get; }
        public SemanticStyle Style { get; }
    }
}
