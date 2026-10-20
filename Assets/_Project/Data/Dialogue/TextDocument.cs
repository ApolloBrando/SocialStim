using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Strict reader for editable narration, results and interface-copy JSON.</summary>
    public static class TextDocument
    {
        /// <summary>Loads version one copy and rejects unknown fields, duplicates, malformed tokens and missing data.</summary>
        public static TextTemplates Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2097152) throw new ArgumentException("Text document is empty or too large.");
            var document = new XmlDocument();
            try
            {
                using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new XmlDictionaryReaderQuotas()))
                    document.Load(reader);
            }
            catch (XmlException exception) { throw new ArgumentException("Malformed text JSON.", exception); }
            XmlElement root = document.DocumentElement;
            Check(root, new[] { "schemaVersion", "entries" });
            if (root["schemaVersion"]?.GetAttribute("type") != "number" || root["schemaVersion"].InnerText != "1"
                || root["entries"]?.GetAttribute("type") != "array") throw new ArgumentException("Text schemaVersion must be 1 with an entries array.");
            var entries = new List<KeyValuePair<string, string>>();
            foreach (XmlElement row in root["entries"].ChildNodes.OfType<XmlElement>())
            {
                Check(row, new[] { "id", "text" });
                if (row["id"]?.GetAttribute("type") != "string" || row["text"]?.GetAttribute("type") != "string")
                    throw new ArgumentException("Text entries need string id/text fields.");
                entries.Add(new KeyValuePair<string, string>(row["id"].InnerText, row["text"].InnerText));
            }
            if (entries.Count == 0) throw new ArgumentException("Text entries cannot be empty.");
            return new TextTemplates(entries);
        }
        private static void Check(XmlElement element, string[] allowed)
        {
            if (element?.GetAttribute("type") != "object") throw new ArgumentException("Expected a text JSON object.");
            string[] names = element.ChildNodes.OfType<XmlElement>().Select(e => e.Name).ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length || names.Any(n => !allowed.Contains(n)))
                throw new ArgumentException("Duplicate or unsupported text JSON field.");
        }
    }
}
