using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIPlanStep
    {
        public CQFAIPlanStep(XElement value)
        {
            Id = (string?)value.Attribute("id") ?? "";
            Status = (string?)value.Attribute("status") ?? "pending";
            Text = value.Element("text")?.Value ?? "";
            Evidence = value.Element("evidence")?.Value ?? "";
            Dependencies = ((string?)value.Attribute("depends") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (value.Name != "step" || Id.Length == 0 || Id.Length > 80 || Id.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-')
                || Status is not ("pending" or "running" or "completed" or "blocked") || string.IsNullOrWhiteSpace(Text) || Text.Length > 2000 || Evidence.Length > 4000
                || Status == "completed" && string.IsNullOrWhiteSpace(Evidence) || value.Attributes().Any(a => a.Name != "id" && a.Name != "status" && a.Name != "depends")
                || value.Elements().Any(e => e.Name != "text" && e.Name != "evidence" || e.HasElements || e.HasAttributes)
                || value.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value))
                || value.Elements().GroupBy(e => e.Name).Any(g => g.Count() > 1)) throw new InvalidDataException("CQF_AI_InvalidPlan");
        }
        public string Id { get; }
        public string Status { get; }
        public string Text { get; }
        public string Evidence { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public XElement Save() => new XElement("step", new XAttribute("id", Id), new XAttribute("status", Status), new XAttribute("depends", string.Join(",", Dependencies)),
            new XElement("text", Text), new XElement("evidence", Evidence));
    }
}
