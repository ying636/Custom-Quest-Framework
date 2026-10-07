using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class AICostChecks
{
    public static async Task Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        CQFAIRequestBudget shared = new(8500, 24);
        using CQFAIRequestLease main = shared.Reserve(100);
        using CQFAIRequestLease worker = shared.Reserve(100);
        Reject(() => shared.Reserve(100), "simultaneous workers cannot reserve the same remaining token budget");
        main.Usage = new CQFAITokenUsage(100, 50, 150); main.Dispose(); worker.Dispose();
        Check(shared.Requests == 2 && shared.Used == 4346 && shared.HasEstimates, "settlement counts actual usage and explicitly estimated missing usage once");
        CQFAIRequestBudget requests = new(100000, 1);
        using (CQFAIRequestLease lease = requests.Reserve(20)) { lease.Rejected = true; }
        Reject(() => requests.Reserve(20), "compatibility and rejected HTTP attempts count toward the shared request limit");
        Check(requests.Used == 0 && requests.Requests == 1, "explicit HTTP rejection adds no fabricated token usage");
        CQFAIRequestBudget overrun = new(10000, 24);
        using (CQFAIRequestLease lease = overrun.Reserve(10)) lease.Usage = new CQFAITokenUsage(11000, 1000, 12000);
        Reject(() => overrun.Reserve(1), "unexpected provider token usage prevents every further automatic request");
        Check(new CustomQuestFramework_ModSetting().dialogAITokenBudget == 100000 && new CustomQuestFramework_ModSetting().dialogAIRequestBudget == 24,
            "new tasks default to shared 100000-token and 24-request budgets");
        DialogTreeDef dialogue = new() { defName = "CQF_Check_CostDialogue" };
        CQFAIConversation dialogueConversation = new(model, catalog);
        CQFAIHarness dialogueHarness = new(model, catalog, dialogueConversation,
            new CQFAIEditorContext(dialogue.defName, () => dialogue, value => dialogue = (DialogTreeDef)value), "CQF_Check_Dialogue", true, true, true);
        dialogueConversation.Add("user", "CQF_Check_Dialogue");
        foreach (Type type in new[] { typeof(DialogTreeDef), typeof(DialogNode), typeof(DialogOption), typeof(DialogResult) })
            Check(dialogueHarness.Instructions.Contains("name=\"" + type.FullName + "\""), "dialogue tool context supplies core schema without a separate model request: " + type.Name);
        dialogueHarness.Process(AIToolChecks.Response("CQF_Check_CostDialogueSchema", "cqf_get_schema", ("type", typeof(DialogOption).FullName!)));
        Check(!dialogueHarness.RequestMessages.Last(message => message.Role == "tool").Content.Contains("<field ")
            && dialogueHarness.Instructions.Split("name=\"" + typeof(DialogOption).FullName + "\"").Length == 2,
            "supplied dialogue schemas appear once rather than in system instructions and tool history");
        Console.WriteLine("DIALOGUE fixture instructions=" + dialogueHarness.Instructions.Length + " totalContext="
            + CQFDialogAIClient.RequestContextLength(dialogueHarness.Instructions, dialogueHarness.RequestMessages, dialogueHarness.Registry.Tools));
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness harness = new(model, catalog, conversation, null, "", false, false, true);
        conversation.Add("user", "CQF_Check_CurrentCommand");
        for (int i = 0; i < 40; i++) harness.Process(AIToolChecks.Response("CQF_Check_RepeatSchema_" + i, "cqf_get_schema", ("type", typeof(InteractionOperation).FullName!)));
        Check(harness.Instructions.Contains("known_schemas") && harness.Instructions.Contains("interactionText"), "resolved editable field schemas remain available across rounds without rediscovery");
        Check(harness.RequestMessages.Sum(message => CQFDialogAIClient.MessageContextLength(message, true)) <= 32000,
            "repeated schema rounds fit the smaller working context while retaining full local history");
        Console.WriteLine("COST fixture instructions=" + harness.Instructions.Length + " history=" + harness.RequestMessages.Sum(message => CQFDialogAIClient.MessageContextLength(message, true))
            + " totalContext=" + CQFDialogAIClient.RequestContextLength(harness.Instructions, harness.RequestMessages, harness.Registry.Tools));
        TcpListener probe = new(IPAddress.Loopback, 0); probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
        int sent = 0;
        Task server = Task.Run(async () =>
        {
            HttpListenerContext request = await listener.GetContextAsync().WaitAsync(timeout.Token); Interlocked.Increment(ref sent);
            byte[] data = Encoding.UTF8.GetBytes("{\"choices\":[{\"message\":{\"content\":\"CQF_Check_Reply\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":20,\"total_tokens\":120}}");
            request.Response.ContentType = "application/json"; await request.Response.OutputStream.WriteAsync(data, timeout.Token); request.Response.Close();
        });
        CQFAIRequestBudget transportBudget = new(100000, 1);
        CQFDialogAIClient first = new("http://127.0.0.1:" + port + "/v1", "CQF_Check_Model", "", 10) { RequestBudget = transportBudget };
        CQFDialogAIClient second = new("http://127.0.0.1:" + port + "/v1", "CQF_Check_Worker", "", 10) { RequestBudget = transportBudget };
        await first.CompleteAsync("CQF_Check_Instructions", "CQF_Check_Command", timeout.Token);
        Reject(() => second.CompleteAsync("CQF_Check_Instructions", "CQF_Check_Worker", timeout.Token).GetAwaiter().GetResult(), "shared limit rejects a worker request before HTTP dispatch");
        await server;
        Check(sent == 1 && transportBudget.Requests == 1 && transportBudget.Used == 120 && !transportBudget.HasEstimates, "actual HTTP usage is settled once and denied requests never reach the provider");
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException error) when (error.Message == "CQF_AI_BudgetPaused") { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException(name);
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
