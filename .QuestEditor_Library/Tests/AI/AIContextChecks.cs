using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIContextChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness harness = new(model, catalog, conversation, null, "CQF_Check_CurrentCommand", false, false);
        conversation.Add("user", "CQF_Check_CurrentCommand" + new string('u', 6000));
        string query = "<assistant><reply>CQF_Check_Reading</reply><queries><schema type='QuestEditor_Library.CustomMapDataDef'/></queries></assistant>";
        for (int round = 0; round < 10; round++)
        {
            Check(harness.Process(query), "large legacy schema query continues: " + round);
            Check(harness.LastResults.Single().Attribute("success")?.Value == "true", "legacy schema query succeeds: " + harness.LastResults.Single().Element("error"));
        }
        Console.WriteLine("Context transcript chars: " + conversation.Messages.Sum(message => (long)message.Content.Length));
        Check(conversation.Messages.Sum(message => (long)message.Content.Length) > 600000,
            "raw tool transcript can exceed the former storage cap without aborting an active task");
        CQFAIMessage[] request = harness.RequestMessages.ToArray();
        Check(request.Sum(message => (long)message.Content.Length) <= 32000 && request.Count(message => message.ToolCalls.Count > 0) <= 2,
            "long schema histories fit the smaller active working context");
        Check(request.Any(message => message.Content == conversation.Messages.First().Content) && request.Last().Content.Contains("legacy_query_10"),
            "small working context preserves the entire command and latest schema result identity");
        string memory = string.Join("", request.Where(message => message.Role == "system").Select(message => message.Content));
        Check(memory.Contains("legacy_query_10") && memory.Contains("QuestEditor_Library.CustomMapDataDef") && memory.Contains("detailsOmitted") && !memory.Contains("<field name="),
            "old schema memory retains call and type identities without replaying field definitions");
        Check(conversation.VisibleMessages.Count() == 11 && conversation.Messages.Count == 21,
            "request compaction preserves full visible history and original stored tool results");
        foreach (CQFAIMessage assistant in request.Where(message => message.ToolCalls.Count > 0))
            Check(assistant.ToolCalls.All(call => request.Count(message => message.Role == "tool" && message.ToolCallId == call.Id) == 1),
                "legacy queries retain complete call/result pairs under context pressure");
        CQFAIConversation grouped = new(model, catalog);
        CQFAIHarness groupedHarness = new(model, catalog, grouped, null, "", false, false, true);
        grouped.Add("user", "CQF_Check_GroupCommand");
        for (int round = 0; round < 2; round++)
        {
            CQFAIToolCall[] schemaCalls = Enumerable.Range(0, 4).Select(index => new CQFAIToolCall("CQF_Check_Group_" + round + "_" + index, "cqf_query_resources",
                new XElement("arguments", new XElement("queries_xml", "<queries><schema type='QuestEditor_Library.CustomMapDataDef'/></queries>")))).ToArray();
            Check(groupedHarness.Process(new XElement("assistant", new XElement("tools", schemaCalls.Select(call => call.ToXml()))).ToString(SaveOptions.DisableFormatting)), "multi-call schema round continues: " + round);
            Check(groupedHarness.LastResults.All(result => result.Attribute("success")?.Value == "true"), "multi-call schema round succeeds: " + round);
        }
        CQFAIMessage[] boundedGroup = grouped.RequestMessages.ToArray();
        Check(boundedGroup.Sum(message => (long)message.Content.Length) < 480000 && boundedGroup.Count(message => message.ToolCalls.Count > 0) == 1
            && boundedGroup.Count(message => message.Role == "tool") == 4, "context pressure retains the latest multi-call round as a complete group");
        Check(boundedGroup.Last().Content == grouped.Messages.Last().Content && boundedGroup.First().Content == "CQF_Check_GroupCommand",
            "adaptive group compaction preserves current command and latest readback verbatim");
        CQFAIConversation arguments = new(model, catalog);
        arguments.Add("user", "CQF_Check_KeepThisCommand");
        CQFAIToolCall[] hugeCalls = Enumerable.Range(0, 6).Select(index => new CQFAIToolCall("CQF_Check_LargeArgument_" + index, "cqf_apply_changes",
            new XElement("arguments", new XElement("changes_xml", new string('x', 110000))))).ToArray();
        arguments.Add("assistant", new XElement("assistant", new XElement("tools", hugeCalls.Select(call => call.ToXml()))).ToString(SaveOptions.DisableFormatting), "", false, hugeCalls);
        foreach (CQFAIToolCall call in hugeCalls)
            arguments.Add("tool", new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true),
                new XElement("applied", new XAttribute("operations", 2), new XAttribute("undoAvailable", true), new XElement("thing", new XAttribute("id", "CQF_Check_ActualThing"))))
                .ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
        CQFAIMessage[] compactedArguments = arguments.RequestMessages.ToArray();
        Check(compactedArguments.Length == 2 && compactedArguments.Sum(message => message.Content.Length) < 5000
            && hugeCalls.All(call => compactedArguments[1].Content.Contains(call.Id)) && compactedArguments[1].Content.Contains("undoAvailable"),
            "a completed oversized multi-call group retains all execution receipts without resending its large arguments");
        Check(compactedArguments[1].Content.Contains("CQF_Check_ActualThing") && !compactedArguments[1].Content.Contains(new string('x', 1000)),
            "compaction preserves actual written object identity and avoids replaying write payloads");
        CQFAIConversation incomplete = new(model, catalog);
        incomplete.Add("assistant", "CQF_Check_Unfinished", "", false, hugeCalls);
        Check(incomplete.RequestMessages.Single().ToolCalls.Count == 6, "unfinished tool groups are preserved even when their arguments are large");
        CQFDialogAIClient client = new("http://127.0.0.1:1/v1", "CQF_Check_Model", "", 10);
        try
        {
            client.CompleteConversationAsync("", incomplete.RequestMessages, CancellationToken.None,
                new[] { new CQFAITool("cqf_apply_changes", "CQF_Check_Tool", _ => new XElement("result")) }).GetAwaiter().GetResult();
            throw new InvalidOperationException("Oversized actual request was accepted.");
        }
        catch (InvalidDataException error)
        {
            Check(error.Message.StartsWith("CQF_AI_RequestTooLarge:", StringComparison.Ordinal) && !client.ReceivedResponse,
                "request limit includes native tool arguments and reports the actual request before network access");
        }
        try { arguments.Add("tool", new string('x', 2097153), visible: false); throw new InvalidOperationException("Oversized single message was accepted."); }
        catch (InvalidDataException error) { Check(error.Message == "CQF_AI_MessageTooLarge", "single-message limits report a distinct cause without clearing history"); }
        Check(arguments.Messages.Count == 8, "rejected single messages preserve previously recorded execution receipts");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
