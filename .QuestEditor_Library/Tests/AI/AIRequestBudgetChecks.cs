using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIRequestBudgetChecks
{
    public static async Task Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        TcpListener probe = new(IPAddress.Loopback, 0); probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(30));
        const string providerModel = "CQF_Check_Budget_Model";
        Task server = Task.Run(async () =>
        {
            for (int index = 0; index < 2; index++)
            {
                HttpListenerContext request = await listener.GetContextAsync().WaitAsync(deadline.Token);
                using JsonDocument body = JsonDocument.Parse(await new StreamReader(request.Request.InputStream, Encoding.UTF8).ReadToEndAsync(deadline.Token));
                JsonElement root = body.RootElement;
                Check(StringCharacters(root) <= CQFDialogAIClient.MaxRequestCharacters, "actual HTTP request fits full context limit: " + index);
                JsonElement[] messages = root.GetProperty("messages").EnumerateArray().ToArray();
                Check(messages.Any(message => message.GetProperty("content").GetString() == "CQF_Check_KeepCommand")
                    && messages.Any(message => message.GetProperty("content").GetString() == "CQF_Check_Steering"), "HTTP compaction preserves original command and steering: " + index);
                string memory = string.Join("", messages.Select(message => message.GetProperty("content").GetString()));
                Check(memory.Contains("CQF_Check_AlreadyApplied") && memory.Contains("undoAvailable") && memory.Contains("CQF_Check_LateReadError"),
                    "HTTP compaction preserves applied edit receipt and errors after large data: " + index);
                Check(memory.Contains("detailsOmitted") && memory.Contains("CQF_Check_BudgetRead_5") && !memory.Contains(new string('r', 10000)),
                    "HTTP compaction marks omitted data and retains latest read identity: " + index);
                foreach (JsonElement toolResult in messages.Where(message => message.GetProperty("role").GetString() == "tool"))
                {
                    string? id = toolResult.GetProperty("tool_call_id").GetString();
                    Check(messages.Count(message => message.TryGetProperty("tool_calls", out JsonElement calls)
                        && calls.EnumerateArray().Any(call => call.GetProperty("id").GetString() == id)) == 1, "HTTP tool results have a complete matching call: " + id);
                }
                byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { role = "assistant", content = "<assistant><reply>CQF_Check_BudgetFinished</reply></assistant>" } } } }));
                request.Response.ContentType = "application/json"; request.Response.ContentLength64 = json.Length;
                await request.Response.OutputStream.WriteAsync(json, deadline.Token); request.Response.Close();
            }
        }, deadline.Token);
        foreach (bool native in new[] { true, false })
        {
            CQFAIConversation conversation = new(model, catalog);
            DialogTreeDef tree = new() { defName = "CQF_Check_BudgetTree" };
            CQFAIHarness harness = new(model, catalog, conversation, new CQFAIEditorContext("CQF_Check_BudgetContext", () => tree, value => tree = (DialogTreeDef)value),
                "CQF_Check_KeepCommand", true, false, native, additionalPrompt: new string('p', 16000));
            conversation.Add("user", "CQF_Check_KeepCommand");
            XElement[] data = Enumerable.Range(0, 6).Select(index => new XElement("readback", new XElement("data", ""),
                new XElement("error", new XAttribute("code", "CQF_Check_LateReadError"), "CQF_Check_Read_" + index))).ToArray();
            int reads = 0;
            harness.Registry.Register(new CQFAITool("cqf_check_budget_read", new string('d', 130000), arguments =>
            {
                reads++;
                return new XElement(data[int.Parse(arguments.Element("index")!.Value)]);
            }, ("index", "CQF_Check_ReadIndex", true)));
            Check(harness.Process(AIToolChecks.Response("CQF_Check_BudgetWrite", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_AlreadyApplied</value></set></changes>")))
                && tree.label == "CQF_Check_AlreadyApplied", "real edit is applied before oversized follow-up: " + native);
            harness.AddInstruction("CQF_Check_Steering");
            CQFAIToolCall[] calls = Enumerable.Range(0, 6).Select(index => new CQFAIToolCall("CQF_Check_BudgetRead_" + index, "cqf_check_budget_read", new XElement("arguments", new XElement("index", index)))).ToArray();
            string response = new XElement("assistant", new XElement("tools", calls.Select(call => call.ToXml()))).ToString(SaveOptions.DisableFormatting);
            CQFAIMessage[] emptyRound = new[] { new CQFAIMessage("assistant", response, "", false, calls) }.Concat(calls.Select((call, index) =>
                new CQFAIMessage("tool", new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true), data[index]).ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id))).ToArray();
            CQFAIMessage additional = harness.RequestMessages.First(message => message.Content == new string('p', 16000));
            CQFAITool[] definitions = native ? harness.Registry.Tools.ToArray() : Array.Empty<CQFAITool>();
            long emptyLength = providerModel.Length + CQFDialogAIClient.RequestContextLength(harness.Instructions,
                new[] { additional }.Concat(conversation.Messages).Concat(emptyRound), definitions);
            int remaining = checked((int)(604469 - emptyLength));
            Check(remaining > 0 && remaining / 6 < 100000, "604469-character request fixture fits individual read limits: " + native);
            for (int index = 0; index < data.Length; index++) data[index].Element("data")!.Value = new string('r', remaining / 6 + (index < remaining % 6 ? 1 : 0));
            Check(harness.Process(response) && reads == 6, "all large reads execute once before request compaction: " + native);
            long rawLength = providerModel.Length + CQFDialogAIClient.RequestContextLength(harness.Instructions, new[] { additional }.Concat(conversation.Messages), definitions);
            Check(rawLength == 604469, "reproduces reported full request size exactly: " + native);
            long storedLength = conversation.Messages.Sum(message => (long)message.Content.Length);
            CQFAIMessage[] bounded = harness.RequestMessages.ToArray();
            long expectedLength = providerModel.Length + CQFDialogAIClient.RequestContextLength(harness.Instructions, bounded, definitions);
            Console.WriteLine("COST large-request fixture raw=" + rawLength + " compacted=" + expectedLength + " native=" + native);
            Check(bounded.Skip(additional.Content.Length > 0 ? 1 : 0).Sum(message => CQFDialogAIClient.MessageContextLength(message, native)) <= 32000,
                "large completed multi-call group fits the smaller working history");
            Check(expectedLength <= CQFDialogAIClient.MaxRequestCharacters && conversation.Messages.Sum(message => (long)message.Content.Length) == storedLength,
                "complete request budget reserves instructions, tools and custom prompt without changing stored history: " + native);
            CQFDialogAIClient client = new("http://127.0.0.1:" + port + "/v1", providerModel, "", 20);
            string answer = await client.CompleteConversationAsync(harness.Instructions, bounded, deadline.Token, definitions);
            Check(new CQFAIResponse(answer).Reply == "CQF_Check_BudgetFinished" && client.ReceivedResponse && reads == 6,
                "oversized follow-up reaches provider and completes without replaying reads or writes: " + native);
            harness.Transaction!.Undo();
            Check(tree.label != "CQF_Check_AlreadyApplied", "full edit undo remains available after request compaction: " + native);
        }
        await server.WaitAsync(deadline.Token);
        CQFAIConversation latest = new(model, catalog);
        latest.Add("user", "CQF_Check_LatestCommand");
        CQFAIToolCall latestCall = new("CQF_Check_SingleLargeResult", "cqf_query_resources", new XElement("arguments", new XElement("queries_xml", "<queries/>")));
        latest.Add("assistant", latestCall.ToXml().ToString(SaveOptions.DisableFormatting), "", false, new[] { latestCall });
        string largeResult = new XElement("tool_result", new XAttribute("id", latestCall.Id), new XAttribute("success", false), new XElement("data", new string('r', 610000)),
            new XElement("runtimeEdited", new XAttribute("undoAvailable", true), new XElement("thing", new XAttribute("id", "CQF_Check_LatestThing"))),
            new XElement("error", new XAttribute("code", "CQF_Check_LastError"), "CQF_Check_LastError")).ToString(SaveOptions.DisableFormatting);
        latest.Add("tool", largeResult, visible: false, toolCallId: latestCall.Id);
        CQFAIMessage[] requestMessages = latest.GetRequestMessages(20000, true).ToArray();
        string summary = string.Join("", requestMessages.Select(message => message.Content));
        Check(requestMessages.Sum(message => CQFDialogAIClient.MessageContextLength(message, true)) <= 20000
            && summary.Contains("CQF_Check_LastError") && summary.Contains("CQF_Check_LatestThing") && summary.Contains("undoAvailable"),
            "oversized latest result preserves trailing error and runtime write identity");
        Check(latest.Messages.Last().Content == largeResult && latest.Messages.Count == 3, "latest result summary does not replace original stored result");
        CQFAIConversation extended = new(model, catalog);
        extended.Add("user", "CQF_Check_ExtendedCommand");
        for (int round = 0; round < 100; round++)
        {
            CQFAIToolCall call = new("CQF_Check_BoundedRound_" + round, "cqf_query_resources", new XElement("arguments", new XElement("queries_xml", "<queries/>")));
            extended.Add("assistant", call.ToXml().ToString(SaveOptions.DisableFormatting), "", false, new[] { call });
            extended.Add("tool", new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("success", true), new XElement("data", new string('r', 9000)))
                .ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
        }
        CQFAIMessage[] longRequest = extended.GetRequestMessages(12000, true).ToArray();
        Check(longRequest.Sum(message => CQFDialogAIClient.MessageContextLength(message, true)) <= 12000
            && longRequest.Any(message => message.Content.Contains("CQF_Check_BoundedRound_99")) && longRequest.Any(message => message.Content.Contains("must not be replayed")),
            "extended tasks bound accumulated tool summaries while retaining latest outcome and replay warning");
        Check(extended.Messages.Count == 201 && extended.Messages.First().Content == "CQF_Check_ExtendedCommand", "extended task compaction retains full recorded history");
    }

    private static long StringCharacters(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()!.Length,
            JsonValueKind.Object => value.EnumerateObject().Sum(property => StringCharacters(property.Value)),
            JsonValueKind.Array => value.EnumerateArray().Sum(StringCharacters),
            _ => 0
        };

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
