using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIStreamResponse
    {
        public CQFAIStreamResponse(Action<CQFAIStreamUpdate> update) { this.update = update; }
        public XElement? Usage { get; private set; }
        public bool Done { get; private set; }
        public void Append(string data)
        {
            if (Done) throw new InvalidDataException("CQF_AI_InvalidStream");
            length += data.Length;
            if (length > 2097152) throw new InvalidDataException("CQF_AI_ResponseTooLarge");
            if (data.Trim() == "[DONE]") { Done = true; Publish(true); return; }
            XElement value = CQFAIJson.Read(data);
            if (value.Element("error") != null) throw new InvalidDataException("CQF_AI_StreamError");
            XElement? usage = value.Element("usage");
            if (usage != null && usage.Attribute("type")?.Value != "null") Usage = new XElement(usage);
            XElement[] choices = value.Element("choices")?.Elements().ToArray() ?? throw new InvalidDataException("CQF_AI_InvalidStream");
            if (choices.Length > 1) throw new InvalidDataException("CQF_AI_InvalidStream");
            if (choices.Length == 0) return;
            XElement choice = choices[0];
            if (choice.Element("index")?.Value is string index && index != "0") throw new InvalidDataException("CQF_AI_InvalidStream");
            string? completion = choice.Element("finish_reason")?.Attribute("type")?.Value == "null" ? null : choice.Element("finish_reason")?.Value;
            if (!string.IsNullOrEmpty(completion))
            {
                if (finish != null) throw new InvalidDataException("CQF_AI_InvalidStream");
                finish = completion;
            }
            XElement? delta = choice.Element("delta");
            if (delta == null || delta.Attribute("type")?.Value != "object") throw new InvalidDataException("CQF_AI_InvalidStream");
            string text = String(delta.Element("content"));
            string thought = String(delta.Element("reasoning_summary"));
            if (thought.Length == 0) thought = String(delta.Element("reasoning_content"));
            if (thought.Length == 0) thought = String(delta.Element("reasoning"));
            if (finishedContent && (text.Length > 0 || thought.Length > 0 || delta.Element("tool_calls")?.HasElements == true)) throw new InvalidDataException("CQF_AI_InvalidStream");
            content.Append(text); reasoning.Append(thought);
            foreach (XElement tool in delta.Element("tool_calls")?.Elements() ?? Enumerable.Empty<XElement>())
            {
                if (!int.TryParse(tool.Element("index")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int position) || position < 0 || position >= 8)
                    throw new InvalidDataException("CQF_AI_InvalidTool");
                if (!calls.TryGetValue(position, out XElement call))
                {
                    call = new XElement("item", new XAttribute("type", "object"), new XElement("id", new XAttribute("type", "string")), new XElement("type", new XAttribute("type", "string"), "function"),
                        new XElement("function", new XAttribute("type", "object"), new XElement("name", new XAttribute("type", "string")), new XElement("arguments", new XAttribute("type", "string"))));
                    calls.Add(position, call);
                }
                string kind = String(tool.Element("type"));
                if (kind.Length > 0 && kind != "function") throw new InvalidDataException("CQF_AI_InvalidTool");
                call.Element("id")!.Value += String(tool.Element("id"));
                call.Element("function")!.Element("name")!.Value += String(tool.Element("function")?.Element("name"));
                call.Element("function")!.Element("arguments")!.Value += String(tool.Element("function")?.Element("arguments"));
                if (call.Element("function")!.Element("arguments")!.Value.Length > 131072) throw new InvalidDataException("CQF_AI_InvalidTool");
            }
            if (finish != null) finishedContent = true;
            Publish(false);
        }
        public XElement Complete()
        {
            if (!Done || finish == null) throw new InvalidOperationException("CQF_DialogAI_Incomplete");
            if (finish != "stop" && finish != "tool_calls") throw new InvalidOperationException("CQF_DialogAI_Incomplete");
            if (calls.Keys.Where((key, index) => key != index).Any()) throw new InvalidDataException("CQF_AI_InvalidTool");
            if (calls.Values.Any(call => string.IsNullOrWhiteSpace(call.Element("id")!.Value) || string.IsNullOrWhiteSpace(call.Element("function")!.Element("name")!.Value)
                || string.IsNullOrWhiteSpace(call.Element("function")!.Element("arguments")!.Value))) throw new InvalidDataException("CQF_AI_InvalidTool");
            Publish(true);
            return new XElement("root", new XAttribute("type", "object"), Usage,
                new XElement("choices", new XAttribute("type", "array"), new XElement("item", new XAttribute("type", "object"),
                    new XElement("finish_reason", new XAttribute("type", "string"), finish), new XElement("message", new XAttribute("type", "object"),
                        new XElement("content", new XAttribute("type", "string"), content.ToString()), calls.Count == 0 ? null : new XElement("tool_calls", new XAttribute("type", "array"), calls.Values)))));
        }
        private void Publish(bool force)
        {
            int stage = calls.Count > 0 ? 3 : content.Length > 0 ? 2 : reasoning.Length > 0 ? 1 : 0;
            if (!force && stage == 0) return;
            if (!force && published && stage == publishedStage && timer.ElapsedMilliseconds < 60) return;
            update(new CQFAIStreamUpdate(content.ToString(), reasoning.ToString(), calls.Values.Select(call => call.Element("function")!.Element("name")!.Value).Where(name => name.Length > 0).ToArray()));
            published = true; publishedStage = stage; timer.Restart();
        }
        private static string String(XElement? element)
        {
            if (element == null || element.Attribute("type")?.Value == "null") return string.Empty;
            if (element.Attribute("type")?.Value != "string") throw new InvalidDataException("CQF_AI_InvalidStream");
            return element.Value;
        }
        private readonly Action<CQFAIStreamUpdate> update;
        private readonly StringBuilder content = new StringBuilder();
        private readonly StringBuilder reasoning = new StringBuilder();
        private readonly SortedDictionary<int, XElement> calls = new SortedDictionary<int, XElement>();
        private readonly Stopwatch timer = Stopwatch.StartNew();
        private string? finish;
        private int length;
        private bool finishedContent;
        private bool published;
        private int publishedStage;
    }
}
