using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIToolNetworkChecks
{
    public static async Task Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        TcpListener probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        CQFAIConversation conversation = new CQFAIConversation(model, catalog);
        object current = AICheckFixtures.Map();
        CustomMapDataDef map = (CustomMapDataDef)current;
        map.customThings.Add(new CustomThingData_InteractableThing { def = DefDatabase<ThingDef>.GetNamed("CQF_Check_Terminal"), position = new IntVec3(4, 0, 4) });
        map.customThings.Add(new CustomThingData_CustomMapEntrance { def = DefDatabase<ThingDef>.GetNamed("CQF_Check_Entrance"), position = new IntVec3(8, 0, 8) });
        map.customThings.Add(new CustomThingData_CustomMapExit { def = DefDatabase<ThingDef>.GetNamed("CQF_Check_Exit"), position = new IntVec3(12, 0, 12) });
        string original = model.Write(current, root: true).ToString();
        CQFAIEditorContext context = new CQFAIEditorContext("CQF_Check_HTTP_Harness", () => current, value => current = value);
        Task server = Task.Run(async () =>
        {
            for (int index = 0; index < 12; index++)
            {
                HttpListenerContext request = await listener.GetContextAsync().WaitAsync(deadline.Token);
                using JsonDocument body = JsonDocument.Parse(await new StreamReader(request.Request.InputStream, Encoding.UTF8).ReadToEndAsync(deadline.Token));
                JsonElement root = body.RootElement;
                JsonElement messages = root.GetProperty("messages");
                Check(request.Request.Headers["Authorization"] == "Bearer CQF_Check_Harness_Key", "harness uses configured provider authorization");
                string? text = null;
                string? tool = null;
                Dictionary<string, string> arguments = new Dictionary<string, string>();
                if (index < 8)
                {
                    Check(!messages[0].GetProperty("content").GetString()!.Contains("In XML transport") && !messages[0].GetProperty("content").GetString()!.Contains("<tools>"), "native prompt omits duplicate XML transport and tool definitions");
                    Check(root.GetProperty("tools").EnumerateArray().Any(definition => definition.GetProperty("function").GetProperty("name").GetString() == "cqf_configure_entrance"), "native requests carry registered function schemas");
                    Check(root.GetProperty("tools").EnumerateArray().All(definition => !definition.GetProperty("function").GetProperty("parameters").GetProperty("additionalProperties").GetBoolean()), "native function schemas reject unknown argument names");
                    if (index > 0)
                    {
                        JsonElement result = messages.EnumerateArray().Last(message => message.GetProperty("role").GetString() == "tool");
                        Check(result.GetProperty("tool_call_id").GetString() == "cqf_http_" + (index - 1), "native result is paired with the requested tool call id");
                        Check(messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "assistant" && message.TryGetProperty("tool_calls", out _)), "native transcript retains assistant function requests");
                    }
                    switch (index)
                    {
                        case 0:
                            tool = "cqf_query_resources"; arguments["queries_xml"] = "<queries><mods/></queries>"; break;
                        case 1:
                            Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("CQF_AI_InvalidQuery"), "native harness returns actionable query errors to the model");
                            tool = "cqf_query_resources"; arguments["queries_xml"] = "<queries><defs type='Verse.ThingDef' search='CQF_Check_Terminal'/></queries>"; break;
                        case 2:
                            Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("CQF.Checks"), "native query feedback includes actual resource origin");
                            tool = "cqf_add_interaction"; arguments["object_path"] = "/customThings/0";
                            arguments["operation_xml"] = "<value Class='QuestEditor_Library.InteractionOperation'><interactionText>CQF_Check_HTTP_Open</interactionText><tickToOperate>150</tickToOperate></value>"; break;
                        case 3:
                            Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("<applied"), "native write reports application before the model continues");
                            tool = "cqf_read_target"; arguments["path"] = "/customThings/0/operations"; arguments["limit"] = "1"; break;
                        case 4:
                            Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("CQF_Check_HTTP_Open"), "native readback verifies the actual edited interaction");
                            tool = "cqf_configure_entrance"; arguments["object_path"] = "/customThings/1";
                            arguments["configuration_xml"] = "<value><data>CQF_Check_Destination</data><exitName>CQF_Check_Return</exitName><opended>false</opended></value>"; break;
                        case 5:
                            tool = "cqf_configure_exit"; arguments["object_path"] = "/customThings/2"; arguments["configuration_xml"] = "<value><exitName>CQF_Check_HTTP_Exit</exitName></value>"; break;
                        case 6:
                            tool = "cqf_validate_target"; break;
                        default:
                            Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("warnings=\"0\""), "native final response follows explicit validation feedback");
                            text = "CQF_Check_HTTP_HarnessDone"; break;
                    }
                }
                else
                {
                    Check(messages[0].GetProperty("content").GetString()!.Contains("In XML transport") && messages[0].GetProperty("content").GetString()!.Contains("<tools>"), "XML prompt retains its actual transport protocol and tool definitions");
                    Check(!root.TryGetProperty("tools", out _) && !messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "tool"), "XML transport works without native function tools");
                    if (index == 8) text = AIToolChecks.Response("xml_read", "cqf_read_target", ("path", "/customThings/0/operations"));
                    else if (index == 9) text = "<assistant><changes><set path='/customThings/0/operations/0/tickToOperate'><value>240</value></set></changes></assistant>";
                    else if (index == 10)
                    {
                        Check(messages.EnumerateArray().Last().GetProperty("content").GetString()!.Contains("<applied"), "legacy XML edits also send execution receipts to the provider");
                        text = AIToolChecks.Response("xml_check", "cqf_validate_target");
                    }
                    else text = "<assistant><reply>CQF_Check_HTTP_XMLDone</reply></assistant>";
                }
                object messageBody = tool == null ? new { role = "assistant", content = text } : (object)new
                {
                    role = "assistant", content = (string?)null,
                    tool_calls = new[] { new { id = "cqf_http_" + index, type = "function", function = new { name = tool, arguments = JsonSerializer.Serialize(arguments) } } }
                };
                object? usage = index switch
                {
                    0 => new { prompt_tokens = 100, completion_tokens = 20, total_tokens = 120, prompt_tokens_details = new { cached_tokens = 30 }, completion_tokens_details = new { reasoning_tokens = 5 } },
                    1 => new { prompt_tokens = 100, completion_tokens = 20, total_tokens = 121 },
                    3 => new { prompt_tokens = 0, completion_tokens = 0, total_tokens = 0 },
                    _ => null
                };
                byte[] response = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = tool == null ? "stop" : "tool_calls", message = messageBody } }, usage }));
                request.Response.ContentType = "application/json"; request.Response.ContentLength64 = response.Length;
                await request.Response.OutputStream.WriteAsync(response, deadline.Token); request.Response.Close();
            }
        }, deadline.Token);
        CQFDialogAIClient client = new CQFDialogAIClient("http://127.0.0.1:" + port + "/v1", "CQF_Check_HarnessModel", "CQF_Check_Harness_Key", 20);
        CQFAIHarness native = new CQFAIHarness(model, catalog, conversation, context, "CQF_Check_HTTP_Command", true, false, true);
        CQFAITokenTotals totals = new CQFAITokenTotals();
        conversation.Add("user", "CQF_Check_HTTP_Command");
        for (int round = 0; ; round++)
        {
            string response = await client.CompleteConversationAsync(native.Instructions, conversation.RequestMessages, deadline.Token, native.Registry.Tools);
            totals.Add(client.LastUsage);
            Check(client.ReceivedResponse, "successful HTTP reply exposes usage availability");
            if (round == 0) Check(client.LastUsage?.Total == 120 && client.LastUsage.Cached == 30 && client.LastUsage.Reasoning == 5, "HTTP usage reads totals and nested details without double counting");
            if (round == 1) Check(client.LastUsage == null && client.UsageError == "CQF_AI_InvalidUsage", "invalid usage is explicit and does not discard valid tool calls");
            if (round == 2) Check(client.LastUsage == null && client.UsageError == null, "missing usage clears previous reply statistics");
            if (round == 3) Check(client.LastUsage?.Total == 0, "reported zero tokens differs from missing usage");
            if (!native.Process(response)) break;
            Check(round < 8, "native harness remains within expected task length");
        }
        Check(native.Rounds == 8 && conversation.VisibleMessages.Last().DisplayContent == "CQF_Check_HTTP_HarnessDone", "native HTTP loop runs through recovery, editing, readback and final reply");
        Check(totals.Input == 100 && totals.Output == 20 && totals.Total == 120 && totals.Reported == 2 && totals.Unavailable == 6, "multi-round usage totals retain unknown replies separately");
        map = (CustomMapDataDef)current;
        Check(((CustomThingData_InteractableThing)map.customThings[0]).operations.Single().tickToOperate == 150
            && !((CustomThingData_CustomMapEntrance)map.customThings[1]).opended && ((CustomThingData_CustomMapExit)map.customThings[2]).exitName == "CQF_Check_HTTP_Exit", "native task applied all requested editor changes");
        XElement nativeState = model.Write(current, root: true);
        native.Transaction!.Undo();
        Check(model.Write(current, root: true).ToString() == original, "one native HTTP task undo restores all of its edits");
        current = model.Read(nativeState, typeof(CustomMapDataDef), current, true)!;
        CQFAIHarness xml = new CQFAIHarness(model, catalog, conversation, context, "CQF_Check_XML_Command", true, false);
        conversation.Add("user", "CQF_Check_XML_Command");
        for (int round = 0; ; round++)
        {
            string response = await client.CompleteConversationAsync(xml.Instructions, conversation.RequestMessages, deadline.Token);
            if (!xml.Process(response)) break;
            Check(round < 4, "XML harness remains within expected task length");
        }
        Check(xml.Rounds == 4 && ((CustomThingData_InteractableThing)((CustomMapDataDef)current).customThings[0]).operations.Single().tickToOperate == 240, "XML HTTP loop performs live edits and checks before completing");
        xml.Transaction!.Undo();
        Check(model.Write(current, root: true).ToString() == nativeState.ToString(), "XML HTTP task undo restores its own starting state");
        await server.WaitAsync(deadline.Token);
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
