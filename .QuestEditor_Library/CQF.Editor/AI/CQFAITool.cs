using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAITool
    {
        public CQFAITool(string name, string description, Func<XElement, XElement> execute, params (string name, string description, bool required)[] parameters)
        {
            Name = name;
            Description = description;
            this.execute = execute;
            this.parameters = parameters;
        }
        public string Name { get; }
        public string Description { get; }
        public XElement Definition => new XElement("tool", new XAttribute("name", Name), new XAttribute("description", Description),
            parameters.Select(parameter => new XElement("parameter", new XAttribute("name", parameter.name), new XAttribute("required", parameter.required), new XAttribute("type", "string"), parameter.description)));
        public XElement JsonDefinition
        {
            get
            {
                XElement properties = new XElement("properties", new XAttribute("type", "object"), parameters.Select(parameter => new XElement(parameter.name, new XAttribute("type", "object"),
                    new XElement("type", new XAttribute("type", "string"), "string"), new XElement("description", new XAttribute("type", "string"), parameter.description))));
                XElement required = new XElement("required", new XAttribute("type", "array"), parameters.Where(parameter => parameter.required)
                    .Select(parameter => new XElement("item", new XAttribute("type", "string"), parameter.name)));
                XElement schema = new XElement("parameters", new XAttribute("type", "object"), new XElement("type", new XAttribute("type", "string"), "object"),
                    new XElement("additionalProperties", new XAttribute("type", "boolean"), "false"), properties, required);
                return new XElement("item", new XAttribute("type", "object"), new XElement("type", new XAttribute("type", "string"), "function"),
                    new XElement("function", new XAttribute("type", "object"), new XElement("name", new XAttribute("type", "string"), Name),
                        new XElement("description", new XAttribute("type", "string"), Description), schema));
            }
        }
        public XElement Execute(XElement arguments)
        {
            if (arguments.Name != "arguments" || arguments.HasAttributes || arguments.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))
                || arguments.Elements().GroupBy(value => value.Name).Any(group => group.Count() > 1)
                || arguments.Elements().Any(value => value.HasAttributes || value.HasElements || !parameters.Any(parameter => parameter.name == value.Name.ToString()))
                || parameters.Any(parameter => parameter.required && string.IsNullOrWhiteSpace(arguments.Element(parameter.name)?.Value)))
                throw new InvalidDataException("CQF_AI_InvalidTool: " + Name);
            return execute(arguments);
        }
        private readonly Func<XElement, XElement> execute;
        private readonly (string name, string description, bool required)[] parameters;
    }
}
