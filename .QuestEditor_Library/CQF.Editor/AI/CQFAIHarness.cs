using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIHarness
    {
        public CQFAIHarness(CQFAIModel model, CQFAIResourceCatalog catalog, CQFAIConversation conversation, CQFAIEditorContext? context, string command, bool editing, bool generateText, bool nativeTools = false)
        {
            this.conversation = conversation;
            conversation.BeginTask();
            Transaction = !editing || context == null ? null : context.Read() is CQFAILiveMapInfo live && live.backend != null
                ? new CQFAILiveMapTransaction(model, context, live.backend) : new CQFAITransaction(model, context);
            Registry = new CQFAIToolRegistry(model, catalog, context, Transaction, command, generateText, editing);
            Instructions = conversation.Instructions(context?.Read(), editing, generateText, Registry, nativeTools);
        }
        public CQFAIToolRegistry Registry { get; }
        public CQFAITransaction? Transaction { get; }
        public string Instructions { get; }
        public int Rounds { get; private set; }
        public int Calls { get; private set; }
        public IReadOnlyList<XElement> LastResults => lastResults;
        public bool Process(string response)
        {
            if (Transaction != null && !Transaction.IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (++Rounds > 24) throw new InvalidOperationException("CQF_AI_ToolLimit");
            lastResults.Clear();
            CQFAIResponse parsed;
            try { parsed = new CQFAIResponse(response); }
            catch (Exception error) when (error is InvalidDataException || error is System.Xml.XmlException)
            {
                if (++failures > 3) throw;
                conversation.Add("system", "The response could not be parsed. Correct the response format. " + error.Message);
                lastResults.Add(Failure("response", "parse", error));
                return true;
            }
            conversation.Add("assistant", response, parsed.Reply, parsed.Reply.Length > 0, parsed.ToolCalls);
            IEnumerable<CQFAIToolCall> calls = parsed.ToolCalls;
            if (parsed.Queries != null) calls = new[] { new CQFAIToolCall("legacy_query_" + Rounds, "cqf_query_resources", new XElement("arguments", new XElement("queries_xml", parsed.Queries.ToString(SaveOptions.DisableFormatting)))) };
            if (parsed.Changes != null) calls = new[] { new CQFAIToolCall("legacy_edit_" + Rounds, "cqf_apply_changes", new XElement("arguments", new XElement("changes_xml", parsed.Changes.ToString(SaveOptions.DisableFormatting)))) };
            bool continued = false;
            foreach (CQFAIToolCall call in calls)
            {
                if (++Calls > 96) throw new InvalidOperationException("CQF_AI_ToolLimit");
                if (!callIds.Add(call.Id)) throw new InvalidDataException("CQF_AI_InvalidTool: duplicate call id");
                XElement result;
                try { result = Registry.Execute(call); }
                catch (InvalidDataException error) when (error.Message.StartsWith("CQF_", StringComparison.Ordinal))
                {
                    result = Failure(call.Id, call.Name, error);
                    if (++failures > 3) throw;
                }
                lastResults.Add(result);
                conversation.Add(parsed.ToolCalls.Count > 0 ? "tool" : "system", result.ToString(SaveOptions.DisableFormatting), visible: false,
                    toolCallId: parsed.ToolCalls.Count > 0 ? call.Id : null);
                continued = true;
            }
            if (!continued && Transaction != null && !Transaction.IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            return continued;
        }
        private static XElement Failure(string id, string name, Exception error)
        {
            string code = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            return new XElement("tool_result", new XAttribute("id", id), new XAttribute("name", name), new XAttribute("success", false), new XElement("error", new XAttribute("code", code), error.Message));
        }
        private readonly CQFAIConversation conversation;
        private readonly List<XElement> lastResults = new List<XElement>();
        private readonly HashSet<string> callIds = new HashSet<string>(StringComparer.Ordinal);
        private int failures;
    }
}
