using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace SocialStim.Data.Dialogue
{
    internal static class DialogueSchema
    {
        internal static void Validate(string json)
        {
            var document = new XmlDocument();
            try
            {
                using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new XmlDictionaryReaderQuotas()))
                    document.Load(reader);
            }
            catch (XmlException exception) { throw new ArgumentException("Malformed dialogue JSON.", exception); }
            XmlElement root = document.DocumentElement;
            CheckObject(root, new[] { "schemaVersion", "lines" }, "document");
            XmlElement lines = root["lines"];
            if (lines == null || lines.GetAttribute("type") != "array") throw new ArgumentException("lines must be an array.");
            foreach (XmlElement line in lines.ChildNodes.OfType<XmlElement>())
            {
                string id = line["id"]?.InnerText ?? "unknown line";
                CheckObject(line, new[] { "id", "intent", "text", "weight", "requiredTrait", "speakerRole", "metadata" }, id);
                foreach (string field in new[] { "id", "intent", "text", "requiredTrait", "speakerRole" })
                    if (line[field] != null && line[field].GetAttribute("type") != "string")
                        throw new ArgumentException($"{id}: {field} must be a string.");
                if (line["metadata"] != null)
                    CheckObject(line["metadata"], new[] { "emotionKey", "portraitKey", "audioKey" }, id + ".metadata");
            }
        }

        private static void CheckObject(XmlElement element, IEnumerable<string> allowed, string location)
        {
            if (element == null || element.GetAttribute("type") != "object") throw new ArgumentException($"{location}: expected an object.");
            var names = element.ChildNodes.OfType<XmlElement>().Select(e => e.Name).ToList();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Count) throw new ArgumentException($"{location}: duplicate JSON property.");
            foreach (string name in names)
                if (!allowed.Contains(name)) throw new ArgumentException($"{location}: unsupported property {name}.");
        }
    }
}
