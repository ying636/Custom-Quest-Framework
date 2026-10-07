using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAITarget
    {
        public CQFAITarget(object identity, string name, string type, string kind, Func<CQFAIEditorContext> resolve, Func<bool> valid, string label = "")
        {
            Identity = identity; Name = name; Type = type; Kind = kind; Resolve = resolve; IsValid = valid; Label = label;
        }
        public object Identity { get; }
        public string Name { get; }
        public string Type { get; }
        public string Kind { get; }
        public string Label { get; internal set; }
        public Func<CQFAIEditorContext> Resolve { get; }
        public Func<bool> IsValid { get; }
        public string Id { get; } = "cqf_target_" + Guid.NewGuid().ToString("N");
        public XElement Summary => new XElement("target", new XAttribute("id", Id), new XAttribute("name", Name), new XAttribute("label", Label), new XAttribute("type", Type), new XAttribute("kind", Kind));
    }
}
