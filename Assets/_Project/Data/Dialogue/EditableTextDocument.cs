using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Text-only JSON editing that preserves every unrelated field, condition and metadata entry.</summary>
    public sealed class EditableTextDocument
    {
        private readonly XmlDocument _document = new XmlDocument();
        private readonly Dictionary<string, XmlElement> _rows;
        /// <summary>Opens a structurally readable spoken/copy document; semantic validation is separate so invalid prose can be repaired.</summary>
        public EditableTextDocument(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2097152) throw new ArgumentException("Empty or oversized text asset.");
            try
            {
                using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new XmlDictionaryReaderQuotas()))
                    _document.Load(reader);
            }
            catch (XmlException exception) { throw new ArgumentException("Malformed text JSON.", exception); }
            XmlElement root = _document.DocumentElement;
            if (root?.GetAttribute("type") != "object") throw new ArgumentException("Expected a text document object.");
            IsCopy = root["entries"] != null;
            XmlElement rows = root[IsCopy ? "entries" : "lines"];
            if (rows?.GetAttribute("type") != "array") throw new ArgumentException("Expected a lines or entries array.");
            _rows = new Dictionary<string, XmlElement>(StringComparer.Ordinal);
            foreach (XmlElement row in rows.ChildNodes.OfType<XmlElement>())
            {
                string id = row["id"]?.InnerText;
                if (string.IsNullOrWhiteSpace(id) || row["id"].GetAttribute("type") != "string"
                    || row["text"]?.GetAttribute("type") != "string" || _rows.ContainsKey(id))
                    throw new ArgumentException("Entries need unique string IDs and text fields.");
                _rows.Add(id, row);
            }
        }
        /// <summary>Whether this asset contains named copy templates rather than spoken variants.</summary>
        public bool IsCopy { get; }
        /// <summary>Stable content IDs in document order.</summary>
        public IReadOnlyList<string> Ids => _rows.Keys.ToList().AsReadOnly();
        /// <summary>Exact editable prose for a row.</summary>
        public string Text(string id) => Row(id)["text"].InnerText;
        /// <summary>Intent or template group for searching.</summary>
        public string Group(string id) => IsCopy ? id.Split('.')[0] : Row(id)["intent"]?.InnerText ?? "";
        /// <summary>Complete row JSON, including conditions and metadata, for inspection.</summary>
        public string Details(string id) => Write(Row(id));
        /// <summary>Canonical metadata JSON for reference generation, excluding the displayed ID/intent/text.</summary>
        public string OtherFields(string id) => "{" + string.Join(", ", Row(id).ChildNodes.OfType<XmlElement>()
            .Where(field => field.Name != "id" && field.Name != "intent" && field.Name != "text").OrderBy(field => field.Name, StringComparer.Ordinal)
            .Select(field => Quote(field.Name) + ": " + Compact(field))) + "}";
        private static string Compact(XmlElement element)
        {
            switch (element.GetAttribute("type"))
            {
                case "object": return "{" + string.Join(", ", element.ChildNodes.OfType<XmlElement>().OrderBy(e => e.Name, StringComparer.Ordinal)
                    .Select(e => Quote(e.Name) + ": " + Compact(e))) + "}";
                case "array": return "[" + string.Join(", ", element.ChildNodes.OfType<XmlElement>().Select(Compact)) + "]";
                case "string": return Quote(element.InnerText);
                case "null": return "null";
                default: return element.InnerText;
            }
        }
        private static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            foreach (char c in value)
                switch (c)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    default: if (c < 32) result.Append("\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture)); else result.Append(c); break;
                }
            return result.Append('"').ToString();
        }
        /// <summary>Returns a changed document without mutating this instance or discarding other fields.</summary>
        public string Reword(string id, string text)
        {
            var copy = new EditableTextDocument(Write(_document.DocumentElement));
            copy.Row(id)["text"].InnerText = text ?? "";
            return Write(copy._document.DocumentElement);
        }
        /// <summary>Checks the same portable schema and copy contracts used by the game.</summary>
        public void Validate()
        {
            string json = Write(_document.DocumentElement);
            if (IsCopy) TextCatalogContract.Validate(TextDocument.Parse(json));
            else DialogueDocument.Parse(json);
        }
        private XmlElement Row(string id) => _rows.TryGetValue(id, out XmlElement row) ? row
            : throw new ArgumentException("Unknown text ID: " + id);
        private static string Write(XmlElement element)
        {
            using (var stream = new MemoryStream())
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                {
                    writer.WriteStartElement("root");
                    foreach (XmlAttribute attribute in element.Attributes) attribute.WriteTo(writer);
                    foreach (XmlNode child in element.ChildNodes) child.WriteTo(writer);
                    writer.WriteEndElement();
                }
                return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n").TrimEnd() + "\n";
            }
        }
    }
}
