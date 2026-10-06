using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

internal static class AINetworkChecks
{
    public static async Task Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        DutyMapDef duty = new DutyMapDef { defName = "CQF_Check_HTTP_Duty" }; duty.CreateNode();
        var cases = new (object source, string changes, Func<object, bool> check)[]
        {
            (new DialogTreeDef { defName = "CQF_Check_HTTP_Dialog" }, "<put path='/nodeMoulds'><key>1</key><value Class='QuestEditor_Library.DialogNode'><index>1</index><text>CQF_Check_Text</text></value></put><append path='/nodeMoulds/@0/options'><value Class='QuestEditor_Library.DialogOption'><text>CQF_Check_Option</text><results><li Class='QuestEditor_Library.DialogResult'><nextIndex>1</nextIndex></li></results></value></append>", value => ((DialogTreeDef)value).nodeMoulds.Count == 2),
            (duty, "<append path='/nodes'><value Class='QuestEditor_Library.DutyMapNode'><nodeId>CQF_Check_HTTP_Second</nodeId></value></append><append path='/transitions'><value Class='QuestEditor_Library.DutyMapTransition'><fromNodeId>" + duty.startNodeId + "</fromNodeId><toNodeId>CQF_Check_HTTP_Second</toNodeId></value></append>", value => ((DutyMapDef)value).transitions.Count == 1),
            (AICheckFixtures.Map(), "<mapResize x='12' z='12'/><terrain def='CQF_Check_Floor' x='0' z='0' width='12' height='12'/><place def='CQF_Check_Item' x='4' z='4' count='2'/>", value => ((CustomMapDataDef)value).thingDatas.Single().count == 2),
            (new ComplexPawnDef { defName = "CQF_Check_HTTP_Pawn" }, "<append path='/modDatas'><value Class='QuestEditor_Library.PawnModData_Basic'/></append>", value => ((ComplexPawnDef)value).modDatas.Count == 1),
            (new QuestBookDef { defName = "CQF_Check_HTTP_Book" }, "<append path='/chapters'><value Class='QuestEditor_Library.QuestBookChapter'><id>CQF_Check_Chapter</id><steps><li Class='QuestEditor_Library.QuestBookStep'><id>CQF_Check_Step</id></li></steps></value></append>", value => ((QuestBookDef)value).chapters.Single().steps.Count == 1),
            (new QuestScriptDef { defName = "CQF_Check_HTTP_Quest", root = new QuestNode_DoCQFActions() }, "<set path='/root/inSignal'><value>$CQF_Check_HTTP_Signal</value></set>", value => ((ISlateRef)((QuestNode_DoCQFActions)((QuestScriptDef)value).root).inSignal).SlateRef == "$CQF_Check_HTTP_Signal"),
            (new DutyDef { defName = "CQF_Check_HTTP_DutyDef", thinkNode = new ThinkNode_Priority() }, "<append path='/thinkNode/subNodes'><value Class='Verse.AI.ThinkNode_Priority'><tag>CQF_Check_HTTP_Tag</tag></value></append>", value => ((DutyDef)value).thinkNode.subNodes.Single().tag == "CQF_Check_HTTP_Tag")
        };
        TcpListener probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Task server = Task.Run(async () =>
        {
            for (int index = 0; index < cases.Length * 2 + 5; index++)
            {
                HttpListenerContext request = await listener.GetContextAsync().WaitAsync(deadline.Token);
                string body = await new StreamReader(request.Request.InputStream, Encoding.UTF8).ReadToEndAsync(deadline.Token);
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement messages = document.RootElement.GetProperty("messages");
                Check(request.Request.Headers["Authorization"] == "Bearer CQF_Check_HTTP_Key", "provider receives configured authorization");
                string response;
                if (index >= cases.Length * 2 + 2)
                {
                    int recovery = index - cases.Length * 2 - 2;
                    if (recovery == 0) response = "<assistant><queries><mods/></queries></assistant>";
                    else if (recovery == 1)
                    {
                        Check(messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "system"
                            && message.GetProperty("content").GetString()!.Contains("<error code=\"CQF_AI_InvalidQuery\"")), "query recovery sends the specific tool error back to the provider");
                        response = "<assistant><queries><defs type='Verse.ThingDef' search='CQF_Check_Item'/></queries></assistant>";
                    }
                    else
                    {
                        Check(messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "system"
                            && message.GetProperty("content").GetString()!.Contains("<def name=\"CQF_Check_Item\"")), "corrected query sends actual resource data to the provider");
                        response = "<assistant><reply>CQF_Check_HTTP_QueryRecovered</reply></assistant>";
                    }
                }
                else if (index >= cases.Length * 2)
                {
                    Check(messages[0].GetProperty("content").GetString()!.Contains("Never return changes"), "chat request enforces read-only context");
                    if (index == cases.Length * 2 + 1) Check(messages.EnumerateArray().Any(message => message.GetProperty("content").GetString()!.Contains("CQF_Check_HTTP_ChatReply")), "follow-up chat retains previous assistant response");
                    response = "<assistant><reply>CQF_Check_HTTP_ChatReply</reply></assistant>";
                }
                else if (index % 2 == 0) response = "<assistant><queries><defs type='Verse.ThingDef' search='CQF_Check_Item'/></queries></assistant>";
                else
                {
                    Check(messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "assistant" && message.GetProperty("content").GetString()!.Contains("queries")), "resource follow-up preserves assistant query");
                    Check(messages.EnumerateArray().Any(message => message.GetProperty("role").GetString() == "system" && message.GetProperty("content").GetString()!.Contains("CQF_Check_Item") && message.GetProperty("content").GetString()!.Contains("CQF.Checks")), "resource follow-up sends actual Def and Mod origin");
                    response = "<assistant><reply>CQF_Check_HTTP_Draft</reply><changes>" + cases[index / 2].changes + "</changes></assistant>";
                }
                byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { role = "assistant", content = response } } } }));
                request.Response.ContentType = "application/json"; request.Response.ContentLength64 = json.Length;
                await request.Response.OutputStream.WriteAsync(json, deadline.Token); request.Response.Close();
            }
        }, deadline.Token);
        CQFDialogAIClient client = new CQFDialogAIClient("http://127.0.0.1:" + port + "/v1", "CQF_Check_HTTP_Model", "CQF_Check_HTTP_Key", 20);
        foreach (var item in cases)
        {
            object current = item.source;
            CQFAITransaction transaction = new CQFAITransaction(model, new CQFAIEditorContext("CQF_Check_HTTP_Target", () => current, value => current = value));
            CQFAIConversation conversation = new CQFAIConversation(model, catalog);
            string instructions = conversation.Instructions(transaction.Source, true, false);
            conversation.Add("user", "CQF_Check_HTTP_Command");
            string queryResponse = await client.CompleteConversationAsync(instructions, conversation.Messages, deadline.Token);
            CQFAIResponse query = new CQFAIResponse(queryResponse);
            conversation.Add("assistant", queryResponse);
            conversation.Add("system", catalog.Query(query.Queries!).ToString(SaveOptions.DisableFormatting));
            CQFAIResponse result = new CQFAIResponse(await client.CompleteConversationAsync(instructions, conversation.Messages, deadline.Token));
            transaction.Build(result.Changes!, "CQF_Check_HTTP_Command", false);
            Check(item.check(transaction.Draft!), "HTTP resource-query-to-draft flow for " + item.source.GetType().Name);
            transaction.Apply(); Check(item.check(current), "HTTP draft application for " + item.source.GetType().Name);
            transaction.Undo(); Check(model.Write(current, root: true).ToString() == model.Write(item.source, root: true).ToString(), "HTTP draft undo for " + item.source.GetType().Name);
        }
        CQFAIConversation chat = new CQFAIConversation(model, catalog);
        chat.Add("user", "CQF_Check_HTTP_Question");
        string chatResponse = await client.CompleteConversationAsync(chat.Instructions(null, false, false), chat.Messages, deadline.Token);
        chat.Add("assistant", chatResponse); chat.Add("user", "CQF_Check_HTTP_Followup");
        Check(new CQFAIResponse(await client.CompleteConversationAsync(chat.Instructions(null, false, false), chat.Messages, deadline.Token)).Changes == null, "continuous chat works without editing target");
        chat.Add("user", "CQF_Check_HTTP_Recovery");
        for (int round = 0; round < 2; round++)
        {
            string response = await client.CompleteConversationAsync(chat.Instructions(null, false, false), chat.Messages, deadline.Token);
            CQFAIResponse query = new CQFAIResponse(response);
            chat.Add("assistant", response, query.Reply, false);
            chat.Add("system", catalog.QueryForAssistant(query.Queries!).ToString(SaveOptions.DisableFormatting));
        }
        CQFAIResponse recovered = new CQFAIResponse(await client.CompleteConversationAsync(chat.Instructions(null, false, false), chat.Messages, deadline.Token));
        Check(recovered.Reply == "CQF_Check_HTTP_QueryRecovered" && recovered.Changes == null, "HTTP invalid query can be corrected and completed in the same conversation");
        await server.WaitAsync(deadline.Token);
    }
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
