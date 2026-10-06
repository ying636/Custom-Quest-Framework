using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIToolCall
    {
        public CQFAIToolCall(string id, string name, XElement arguments, string? rawArguments = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 120 || string.IsNullOrWhiteSpace(name) || name.Length > 100
                || arguments.Name != "arguments" || arguments.HasAttributes || arguments.ToString().Length > 131072 || rawArguments?.Length > 131072)
                throw new InvalidDataException("CQF_AI_InvalidTool");
            Id = id;
            Name = name;
            Arguments = new XElement(arguments);
            this.rawArguments = rawArguments;
        }
        public string Id { get; }
        public string Name { get; }
        public XElement Arguments { get; }
        public XElement ToXml() => new XElement("call", new XAttribute("id", Id), new XAttribute("name", Name), rawArguments == null ? null : new XAttribute("json", rawArguments), new XElement(Arguments));
        public XElement ToJson()
        {
            return new XElement("item", new XAttribute("type", "object"),
                new XElement("id", new XAttribute("type", "string"), Id), new XElement("type", new XAttribute("type", "string"), "function"),
                new XElement("function", new XAttribute("type", "object"), new XElement("name", new XAttribute("type", "string"), Name),
                    new XElement("arguments", new XAttribute("type", "string"), rawArguments ?? CQFAIJson.Write(new XElement("root", new XAttribute("type", "object"),
                        Arguments.Elements().Select(value => new XElement(value.Name, new XAttribute("type", "string"), value.Value)))))));
        }
        public static CQFAIToolCall FromXml(XElement call)
        {
            if (call.Name != "call" || call.Attributes().Any(attribute => attribute.Name != "id" && attribute.Name != "name" && attribute.Name != "json")
                || call.Elements().Count() != 1 || call.Element("arguments") == null || call.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("CQF_AI_InvalidTool");
            return new CQFAIToolCall(call.Attribute("id")?.Value ?? "", call.Attribute("name")?.Value ?? "", call.Element("arguments")!, call.Attribute("json")?.Value);
        }
        public static CQFAIToolCall FromJson(XElement call)
        {
            if (call.Element("type")?.Value != "function") throw new InvalidDataException("CQF_AI_InvalidTool");
            XElement function = call.Element("function") ?? throw new InvalidDataException("CQF_AI_InvalidTool");
            string raw = function.Element("arguments")?.Value ?? "";
            if (raw.Length > 131072) throw new InvalidDataException("CQF_AI_InvalidTool");
            XElement arguments;
            try { arguments = CQFAIJson.Read(raw); }
            catch (Exception error) when (error is System.Xml.XmlException || error is System.Runtime.Serialization.SerializationException)
            { arguments = new XElement("root", new XAttribute("type", "invalid")); }
            if (arguments.Attribute("type")?.Value != "object" || arguments.Elements().Any(value => value.HasElements || value.Attribute("type")?.Value != "string"))
                return new CQFAIToolCall(call.Element("id")?.Value ?? "", function.Element("name")?.Value ?? "",
                    new XElement("arguments", new XElement("CQF_InvalidToolArguments", "Arguments must be a JSON object containing string values.")), raw);
            return new CQFAIToolCall(call.Element("id")?.Value ?? "", function.Element("name")?.Value ?? "",
                new XElement("arguments", arguments.Elements().Select(value => new XElement(value.Name, value.Value))), raw);
        }
        private readonly string? rawArguments;
    }
}
