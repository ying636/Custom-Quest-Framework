using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AITokenChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        CQFAITokenUsage usage = CQFAITokenUsage.Read(CQFAIJson.Read("{\"usage\":{\"prompt_tokens\":300,\"completion_tokens\":20,\"total_tokens\":320,\"prompt_tokens_details\":{\"cached_tokens\":100},\"completion_tokens_details\":{\"reasoning_tokens\":5}}}").Element("usage"))!;
        Check(usage.Input == 300 && usage.Output == 20 && usage.Total == 320 && usage.Cached == 100 && usage.Reasoning == 5, "usage details are subsets of reported input/output");
        Check(CQFAITokenUsage.Read(null) == null && CQFAITokenUsage.Read(CQFAIJson.Read("{\"usage\":null}").Element("usage")) == null, "absent and null usage remain unknown");
        foreach (string invalid in new[] { "{}", "{\"prompt_tokens\":-1,\"completion_tokens\":0,\"total_tokens\":0}", "{\"prompt_tokens\":1.5,\"completion_tokens\":0,\"total_tokens\":1}",
            "{\"prompt_tokens\":\"1\",\"completion_tokens\":0,\"total_tokens\":1}", "{\"prompt_tokens\":1,\"completion_tokens\":1,\"total_tokens\":1}",
            "{\"prompt_tokens\":9223372036854775807,\"completion_tokens\":1,\"total_tokens\":0}",
            "{\"prompt_tokens\":1,\"completion_tokens\":0,\"total_tokens\":1,\"prompt_tokens_details\":{\"cached_tokens\":2}}" })
            Reject(() => CQFAITokenUsage.Read(CQFAIJson.Read(invalid)), "invalid usage rejected: " + invalid);
        CQFAITokenTotals totals = new CQFAITokenTotals(); totals.Add(usage); totals.Add(null); totals.Add(new CQFAITokenUsage(0, 0, 0));
        Check(totals.Total == 320 && totals.Reported == 2 && totals.Unavailable == 1, "usage aggregation distinguishes reported zero from unavailable");
        totals.Clear(); Check(totals.Total == 0 && totals.Reported == 0 && totals.Unavailable == 0, "clearing chat resets usage counters");
        CQFAIConversation conversation = new CQFAIConversation(model, catalog);
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Check_Token_Dialog" };
        CQFAIEditorContext context = new CQFAIEditorContext("CQF_Check_Token_Context", () => tree, value => tree = (DialogTreeDef)value);
        CQFAIHarness native = new CQFAIHarness(model, catalog, conversation, context, "", true, false, true);
        string xml = conversation.Instructions(tree, true, false, native.Registry);
        Check(native.Instructions.Length < xml.Length && !native.Instructions.Contains("In XML transport") && !native.Instructions.Contains("<tools>"), "native prompt sends function definitions only once");
        Check(!native.Instructions.Contains("<mods>") && !native.Instructions.Contains("<defTypes>") && !native.Instructions.Contains("<schema>"), "initial prompt defers full resource directories and field schemas");
        Check(native.Instructions.Contains("cqf_get_schema") && native.Instructions.Contains("/nodeMoulds"), "compact prompt retains target paths and schema discovery");
        Check(!native.Instructions.Contains("<mapResize"), "dialogue prompt omits map draft operations");
        Console.WriteLine("Native dialogue instruction characters: " + native.Instructions.Length + "; XML characters: " + xml.Length);
        CQFAITool mods = native.Registry.Tools.Single(tool => tool.Name == "cqf_list_mods");
        Check(mods.Execute(new XElement("arguments", new XElement("search", "CQF"))).Elements("mod").Any(mod => mod.Attribute("id")?.Value == "CQF.Checks"), "on-demand Mod discovery returns actual package IDs");
        Check(!mods.Execute(new XElement("arguments", new XElement("offset", "100000"))).Elements().Any(), "Mod discovery obeys page offset");
        CQFAITool types = native.Registry.Tools.Single(tool => tool.Name == "cqf_list_def_types");
        Check(types.Execute(new XElement("arguments", new XElement("search", "ThingDef"))).Elements("type").Any(type => type.Attribute("name")?.Value == "Verse.ThingDef"), "Def discovery includes native types outside editable CQF model");
        Reject(() => types.Execute(new XElement("arguments", new XElement("offset", "-1"))), "resource discovery rejects invalid pagination");
        XElement overview = native.Registry.Tools.Single(tool => tool.Name == "cqf_get_context").Execute(new XElement("arguments"));
        Check(overview.Element("resources")?.Attribute("mods") != null && !overview.Descendants("mod").Any(), "context refresh carries counts instead of full Mod list");
        conversation.Add("user", "CQF_Check_CurrentCommand" + new string('u', 6000));
        for (int round = 0; round < 4; round++)
        {
            CQFAIToolCall[] calls = Enumerable.Range(0, 2).Select(index => new CQFAIToolCall("CQF_Check_Call_" + round + "_" + index, "cqf_apply_changes",
                new XElement("arguments", new XElement("changes_xml", new string('x', 10000))))).ToArray();
            string response = new XElement("assistant", new XElement("reply", "CQF_Check_Visible_" + round), new XElement("tools", calls.Select(call => call.ToXml()))).ToString(SaveOptions.DisableFormatting);
            conversation.Add("assistant", response, "CQF_Check_Visible_" + round, true, calls);
            foreach (CQFAIToolCall call in calls)
            {
                XElement result = new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", round != 0),
                    round == 0 ? new XElement("error", new XAttribute("code", "CQF_AI_InvalidPath"), "CQF_Check_Field")
                    : new XElement("applied", new XAttribute("undoAvailable", true), new XElement("description", "CQF_Check_EditedDescription"), new XElement("thing", new XAttribute("id", "CQF_Check_Thing_" + round), new XElement("path", "/operations"))));
                conversation.Add("tool", result.ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
            }
        }
        CQFAIMessage[] request = conversation.RequestMessages.ToArray();
        Check(request.Sum(message => message.Content.Length) + 30000 < conversation.Messages.Sum(message => message.Content.Length), "completed large tool arguments are omitted from subsequent requests");
        Check(request.Count(message => message.ToolCalls.Count > 0) == 2 && request.Count(message => message.Role == "tool") == 4, "latest two multi-call rounds remain complete");
        Check(request.Last().Content == conversation.Messages.Last().Content && request.First().Content.Length > 6000, "latest tool results and current command are never truncated");
        string memory = string.Join("", request.Where(message => message.Role == "system").Select(message => message.Content));
        Check(memory.Contains("CQF_AI_InvalidPath") && memory.Contains("CQF_Check_Thing_1") && memory.Contains("undoAvailable") && memory.Contains("CQF_Check_Call_0_1"), "compacted memory retains errors, write receipts, object and call IDs");
        Check(memory.Contains("CQF_Check_EditedDescription"), "compaction preserves actual edited narrative fields");
        foreach (CQFAIMessage assistant in request.Where(message => message.ToolCalls.Count > 0))
            Check(assistant.ToolCalls.All(call => request.Count(message => message.Role == "tool" && message.ToolCallId == call.Id) == 1), "retained calls have exactly one paired result");
        Check(conversation.VisibleMessages.Count() == 5 && conversation.Messages.Count == 13, "request compaction preserves full chat history and stored transcript");
        CQFAIConversation unfinished = new CQFAIConversation(model, catalog);
        for (int round = 0; round < 4; round++)
        {
            CQFAIToolCall call = new CQFAIToolCall("pending_" + round, "cqf_read_target", new XElement("arguments", new XElement("path", "/")));
            unfinished.Add("assistant", call.ToXml().ToString(), "", false, new[] { call });
        }
        Check(unfinished.RequestMessages.Count() == 4 && unfinished.RequestMessages.All(message => message.ToolCalls.Count == 1), "incomplete tool groups are never compacted");
        for (int index = 0; index < 12; index++) conversation.Add(index % 2 == 0 ? "user" : "assistant", "CQF_Check_Old_" + index + new string('y', 3000));
        conversation.BeginTask(); conversation.Add("user", "CQF_Check_Next");
        Check(conversation.RequestMessages.Count() == 7 && conversation.RequestMessages.Take(6).All(message => message.Content.Length < 1650), "earlier tasks use six bounded excerpts instead of replaying full transcripts");
        Check(conversation.VisibleMessages.Count() == 18, "earlier-task excerpt limits do not shorten visible history");
        CQFAIConversation descriptions = new CQFAIConversation(model, catalog);
        for (int round = 0; round < 4; round++)
        {
            CQFAIToolCall call = new CQFAIToolCall("description_" + round, "cqf_query_resources", new XElement("arguments", new XElement("queries_xml", "<queries><defs type='Verse.ThingDef'/></queries>")));
            descriptions.Add("assistant", new XElement("assistant", new XElement("tools", call.ToXml())).ToString(), "", false, new[] { call });
            XElement result = new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true),
                new XElement("results", new XElement("defs", new XAttribute("total", 1), new XElement("def", new XAttribute("name", "CQF_Check_Discovered"), new XAttribute("mod", "CQF.Checks"),
                    new XElement("description", new string('d', 4000)), new XElement("placement", new XAttribute("madeFromStuff", true), new XElement("stuff", "CQF_Check_Wood"))))));
            descriptions.Add("tool", result.ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
        }
        CQFAIMessage[] resourceHistory = descriptions.RequestMessages.ToArray();
        Check(resourceHistory.Where(message => message.Role == "system").All(message => !message.Content.Contains(new string('d', 100)))
            && resourceHistory.Where(message => message.Role == "tool").All(message => message.Content.Contains(new string('d', 4000))), "old Def descriptions are removed after recent full readbacks remain available");
        Check(resourceHistory.Where(message => message.Role == "system").All(message => message.Content.Contains("CQF_Check_Discovered") && message.Content.Contains("CQF.Checks") && message.Content.Contains("CQF_Check_Wood")), "compacted resource queries retain actual names, Mod origin and material metadata");
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
