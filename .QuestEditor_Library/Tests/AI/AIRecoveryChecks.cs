using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIRecoveryChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        object current = new DialogTreeDef { defName = "CQF_Check_Recovery" };
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness harness = new(model, catalog, conversation, new CQFAIEditorContext("CQF_Check_Recovery", () => current, value => current = value), "", true, false);
        int calls = 0;
        harness.Registry.Register(new CQFAITool("cqf_check_conflict", "CQF_Check_Conflict", _ =>
        {
            calls++;
            throw new InvalidDataException("CQF_AI_LiveMapBlocked: CQF_Check_Conflict anchor=(5,6) rotation=1 footprint=(minX=4,minZ=6,width=3,height=2)");
        }));
        for (int i = 0; i < 5; i++)
        {
            Check(harness.Process(AIToolChecks.Response("CQF_Check_Conflict_" + i, "cqf_check_conflict")), "recoverable placement conflicts return feedback without ending the task: " + i);
            XElement result = harness.LastResults.Single();
            Check(result.Attribute("success")?.Value == "false" && result.Element("error")?.Attribute("code")?.Value == "CQF_AI_LiveMapBlocked"
                && conversation.RequestMessages.Last().Content.Contains("anchor=(5,6)"), "the failed tool receipt preserves conflict geometry for the next model request: " + i);
        }
        Check(calls == 5 && !harness.Transaction!.CanUndo, "rejected tool attempts do not create an undo record or modify the target");
        harness.Process(AIToolChecks.Response("CQF_Check_Repair", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_Repaired</value></set></changes>")));
        Check(((DialogTreeDef)current).label == "CQF_Check_Repaired" && harness.Transaction!.CanUndo, "a corrected edit applies after more than three tool failures");
        harness.Transaction!.Undo();
        Check(((DialogTreeDef)current).label != "CQF_Check_Repaired", "the corrected edit remains undoable as one task");
        Check(!harness.Process("<assistant><reply>CQF_Check_Finished</reply></assistant>"), "a repaired request finishes when the model returns its final reply");
        CQFAIConversation extendedConversation = new(model, catalog);
        CQFAIHarness extended = new(model, catalog, extendedConversation, new CQFAIEditorContext("CQF_Check_Recovery", () => current, value => current = value), "", true, false);
        int completedCalls = 0;
        extended.Registry.Register(new CQFAITool("cqf_check_progress", "CQF_Check_Progress", _ =>
        {
            completedCalls++;
            return new XElement("tool_result", new XAttribute("success", true), new XElement("progress", completedCalls));
        }));
        CQFAIActivity extendedActivity = new(0);
        extended.ToolProgress = (call, result) =>
        {
            if (result == null) extendedActivity.Add(new CQFAIOperation(call));
            else extendedActivity.Operations.Last().Complete(result, "");
        };
        for (int round = 0; round < 40; round++)
        {
            CQFAIToolCall[] batch = Enumerable.Range(0, 4).Select(index => new CQFAIToolCall("CQF_Check_Progress_" + round + "_" + index, "cqf_check_progress", new XElement("arguments"))).ToArray();
            Check(extended.Process(new XElement("assistant", new XElement("tools", batch.Select(call => call.ToXml()))).ToString(SaveOptions.DisableFormatting)), "a progressing task continues without a fixed round or call cutoff: " + round);
        }
        Check(extended.Rounds == 40 && extended.Calls == 160 && completedCalls == 160, "all calls execute beyond the previous 24-round and 96-call limits");
        CQFAIMessage[] extendedRequest = extendedConversation.RequestMessages.ToArray();
        Check(extendedRequest.Count(message => message.ToolCalls.Count > 0) == 2 && extendedRequest.Count(message => message.Role == "tool") == 8,
            "extended tasks retain complete recent tool groups while compacting old results");
        string originalLabel = ((DialogTreeDef)current).label;
        Check(extended.Process(AIToolChecks.Response("CQF_Check_ExtendedEdit", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_ExtendedEdit</value></set></changes>")))
            && ((DialogTreeDef)current).label == "CQF_Check_ExtendedEdit" && extended.Transaction!.CanUndo, "a real edit applies after more than 96 tool calls and remains undoable");
        extended.Transaction!.Undo();
        Check(((DialogTreeDef)current).label == originalLabel, "undo restores the actual target after an extended task");
        Check(!extended.Process("<assistant><reply>CQF_Check_ExtendedFinished</reply></assistant>"), "an extended task ends normally on a final response");
        extendedActivity.Complete();
        CQFAIActivity restoredActivity = CQFAIActivity.Restore(extendedActivity.Save());
        Check(restoredActivity.Operations.Count == 161 && restoredActivity.Operations.All(operation => operation.Succeeded == true), "extended task history restores all operations beyond the previous 96-operation limit");
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
