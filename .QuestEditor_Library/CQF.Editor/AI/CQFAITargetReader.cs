using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAITargetReader
    {
        public CQFAITargetReader(CQFAIModel model) { this.model = model; }
        public XElement Summary(object? target)
        {
            if (target == null) return new XElement("target", new XAttribute("available", false));
            return SummaryValue(model.Write(target, root: true), "/");
        }
        public XElement Read(object target, string path, int offset = 0, int limit = 20)
        {
            if (offset < 0 || offset > 100000 || limit < 1 || limit > 40) throw new InvalidDataException("CQF_AI_InvalidTool: pagination");
            XElement value = Find(model.Write(target, root: true), path);
            bool collection = value.Elements().Any(element => element.Name == "li" || element.Name == "entry");
            if (!collection && offset != 0) throw new InvalidDataException("CQF_AI_InvalidTool: offset requires a collection");
            XElement result = new XElement("data", new XAttribute("path", path));
            if (collection)
            {
                result.Add(new XAttribute("total", value.Elements().Count()), new XAttribute("offset", offset));
                value = new XElement(value.Name, value.Attributes(), value.Elements().Skip(offset).Take(limit).Select(element => new XElement(element)));
            }
            result.Add(value);
            if (result.ToString(SaveOptions.DisableFormatting).Length > 65536) throw new InvalidDataException("CQF_AI_ReadTooLarge");
            return result;
        }
        public static XElement Find(XElement root, string path)
        {
            if (path == "/") return root;
            if (!path.StartsWith("/", StringComparison.Ordinal) || path.Length > 500 || path.Contains("//")) throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
            XElement current = root;
            foreach (string segment in path.Substring(1).Split('/'))
            {
                if (segment.StartsWith("@", StringComparison.Ordinal))
                    current = current.Elements("entry").FirstOrDefault(entry => entry.Element("key")?.Value == Uri.UnescapeDataString(segment.Substring(1)))?.Element("value")
                        ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
                else if (int.TryParse(segment, out int index)) current = current.Elements("li").ElementAtOrDefault(index) ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
                else current = current.Element(segment) ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
            }
            return current;
        }
        private static XElement SummaryValue(XElement value, string path)
        {
            XElement result = new XElement("target", new XAttribute("path", path), value.Attributes().Select(attribute => new XAttribute(attribute)));
            foreach (XElement field in value.Elements())
            {
                XElement info = new XElement("field", new XAttribute("name", field.Name), new XAttribute("path", path.TrimEnd('/') + "/" + field.Name), field.Attributes().Select(attribute => new XAttribute(attribute)));
                if (!field.HasElements) info.Value = field.Value.Length <= 180 ? field.Value : field.Value.Substring(0, 180) + "…";
                else
                {
                    info.Add(new XAttribute("count", field.Elements().Count()));
                    if (field.Elements().All(item => item.Name == "li" || item.Name == "entry"))
                        info.Add(field.Elements().Take(5).Select((item, index) => new XElement("item", new XAttribute("path", info.Attribute("path")!.Value + "/"
                            + (item.Name == "entry" ? "@" + Uri.EscapeDataString(item.Element("key")!.Value) : index.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                            (item.Name == "entry" ? item.Element("value")! : item).Attributes().Select(attribute => new XAttribute(attribute)),
                            (item.Name == "entry" ? item.Element("value")! : item).Elements().Where(child => child.Name.LocalName is "def" or "position" or "exitName" or "interactionText" or "nodeId" or "id").Select(child => new XElement(child)))));
                }
                result.Add(info);
            }
            return result;
        }
        private readonly CQFAIModel model;
    }
}
