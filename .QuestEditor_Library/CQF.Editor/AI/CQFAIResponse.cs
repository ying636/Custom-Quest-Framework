using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIResponse
    {
        public CQFAIResponse(string response)
        {
            XElement root = CQFAIChanges.Parse(response);
            if (root.Name != "assistant" || root.HasAttributes || root.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))
                || root.Elements().Any(element => element.Name != "reply" && element.Name != "queries" && element.Name != "changes" && element.Name != "tools")
                || root.Elements().GroupBy(element => element.Name).Any(group => group.Count() > 1) || root.Element("reply")?.HasElements == true || root.Element("reply")?.HasAttributes == true
                || root.Elements().Count(element => element.Name != "reply") > 1)
                throw new InvalidDataException("CQF_AI_InvalidResponse");
            Reply = root.Element("reply")?.Value ?? string.Empty;
            Queries = root.Element("queries");
            Changes = root.Element("changes");
            XElement? tools = root.Element("tools");
            if (tools != null && (tools.HasAttributes || tools.Elements().Count() < 1 || tools.Elements().Count() > 8 || tools.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))))
                throw new InvalidDataException("CQF_AI_InvalidTool");
            ToolCalls = tools?.Elements().Select(CQFAIToolCall.FromXml).ToArray() ?? Array.Empty<CQFAIToolCall>();
            if (ToolCalls.Select(call => call.Id).Distinct().Count() != ToolCalls.Count) throw new InvalidDataException("CQF_AI_InvalidTool: duplicate call id");
            if (Reply.Length == 0 && Queries == null && Changes == null && ToolCalls.Count == 0) throw new InvalidDataException("CQF_AI_InvalidResponse");
        }
        public string Reply { get; }
        public XElement? Queries { get; }
        public XElement? Changes { get; }
        public IReadOnlyList<CQFAIToolCall> ToolCalls { get; }
    }
}
