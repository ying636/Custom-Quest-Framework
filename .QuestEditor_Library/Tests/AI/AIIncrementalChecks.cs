using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIIncrementalChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        AIFakeLiveMap map = new(model);
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness harness = new(model, catalog, conversation, CQFAILiveMapContext.Create(map), "", true, false);
        string changes = "<changes>" + string.Concat(Enumerable.Range(0, 24).Select(x => $"<terrain def='CQF_Check_Floor' x='{x}' z='3'/>")) + "</changes>";
        int applied = 0;
        harness.MapStepApplied = (_, _) => applied++;
        harness.BeginResponse(AIToolChecks.Response("CQF_Check_Incremental", "cqf_apply_changes", ("changes_xml", changes)));
        Check(map.Cells.Count == 0 && harness.HasPendingResponse, "queued map response does not mutate the map before a UI frame");
        Check(!harness.AdvanceResponse(1, 20) && map.Cells.Count == 1 && applied == 1, "first UI frame applies only one operation and reports actual feedback");
        Check(harness.ActiveMapExecution?.CompletedOperations == 1 && harness.ActiveMapExecution.TotalOperations == 24, "map execution exposes exact current operation counts");
        harness.AdvanceResponse(1, 20);
        harness.CancelResponse();
        Check(map.Cells.Count == 2 && harness.Transaction!.CanUndo && !harness.HasPendingResponse, "stopping preserves completed changes and task undo");
        Check(conversation.Messages.Count(message => message.Role == "tool" && message.ToolCallId == "CQF_Check_Incremental") == 1,
            "stopping closes the active native call with one result");
        harness.Transaction!.Undo();
        Check(map.Cells.Count == 0, "undo restores the state before a partially completed queued batch");
        harness.BeginResponse(AIToolChecks.Response("CQF_Check_CompleteIncremental", "cqf_apply_changes", ("changes_xml", changes)));
        int frames = Drain(harness, map);
        Check(frames >= 24 && map.Cells.Count == 24 && harness.ResponseContinued && harness.LastResults.Single().Descendants("liveMapApplied").Single().Attribute("operations")!.Value == "24",
            "paused-game-independent frame execution produces an actual final receipt");
        map.Cells["terrain:0:3"] = "CQF_Check_PlayerChange";
        harness.BeginResponse(AIToolChecks.Response("CQF_Check_UnrelatedEdit", "cqf_apply_changes", ("changes_xml", "<changes><roof def='CQF_Check_Roof' x='30' z='3'/></changes>")));
        harness.AdvanceResponse(1, 20); harness.CancelResponse();
        Check(!harness.Transaction.IsCurrent && map.Cells["terrain:0:3"] == "CQF_Check_PlayerChange", "stopping another batch never absorbs unrelated manual map changes");
        AIFakeLiveMap failed = new(model) { FailKey = "roof:4:3" };
        CQFAIHarness recovering = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(failed), "", true, false);
        recovering.BeginResponse(AIToolChecks.Response("CQF_Check_QueuedFailure", "cqf_apply_changes", ("changes_xml", "<changes><terrain def='CQF_Check_Floor' x='3' z='3'/><roof def='CQF_Check_Roof' x='4' z='3'/></changes>")));
        Drain(recovering, failed);
        Check(failed.Cells.Count == 0 && recovering.LastResults.Single().Attribute("success")!.Value == "false" && !recovering.Transaction!.CanUndo,
            "queued failures roll back step by step and return visible failure receipts");
        CQFAIHarness sequential = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(new AIFakeLiveMap(model)), "", true, false);
        int reads = 0;
        sequential.Registry.Register(new CQFAITool("cqf_check_queued_read", "CQF_Check", _ => { reads++; return new XElement("read"); }));
        CQFAIToolCall write = new("CQF_Check_WriteFirst", "cqf_apply_changes", new XElement("arguments", new XElement("changes_xml", changes)));
        CQFAIToolCall read = new("CQF_Check_ReadSecond", "cqf_check_queued_read", new XElement("arguments"));
        sequential.BeginResponse(new XElement("assistant", new XElement("tools", write.ToXml(), read.ToXml())).ToString(SaveOptions.DisableFormatting));
        sequential.AdvanceResponse(1, 20);
        Check(reads == 0, "later tools wait for queued writes instead of reading an incomplete map");
        while (!sequential.AdvanceResponse(1, 20)) { }
        Check(reads == 1 && sequential.LastResults.Count == 2, "later tools execute once after map writes complete");
        AIFakeLiveMap slow = new(model) { ApplyDelayMilliseconds = 5 };
        CQFAIHarness timeLimited = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(slow), "", true, false);
        timeLimited.BeginResponse(AIToolChecks.Response("CQF_Check_FrameBudget", "cqf_apply_changes", ("changes_xml", changes)));
        timeLimited.AdvanceResponse(1000, 2);
        Check(slow.Cells.Count == 1 && timeLimited.HasPendingResponse, "frame time budget prevents a second expensive native-style operation");
        timeLimited.CancelResponse(); timeLimited.Transaction!.Undo();
        Check(slow.Cells.Count == 0, "time-sliced immediate mode retains cancellation and undo");
        AIFakeLiveMap invalidated = new(model);
        CQFAIHarness targetChanged = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(invalidated), "", true, false);
        targetChanged.BeginResponse(AIToolChecks.Response("CQF_Check_InvalidateQueue", "cqf_apply_changes", ("changes_xml", changes)));
        targetChanged.AdvanceResponse(1, 20); invalidated.IsValid = false;
        bool rejected = false;
        try { while (!targetChanged.AdvanceResponse(1, 20)) { } }
        catch (InvalidOperationException error) when (error.Message == "CQF_AI_StaleTarget") { rejected = true; }
        targetChanged.CancelResponse();
        Check(rejected && invalidated.Cells.Count == 0 && !targetChanged.HasPendingResponse,
            "invalidated map targets stop queued writes, restore their batch and report failure");
    }
    private static int Drain(CQFAIHarness harness, AIFakeLiveMap map)
    {
        int frames = 0;
        while (harness.HasPendingResponse)
        {
            int before = map.Cells.Count;
            harness.AdvanceResponse(1, 20);
            Check(Math.Abs(map.Cells.Count - before) <= 1, "one frame never applies or rolls back multiple requested changes");
            if (++frames > 100) throw new InvalidOperationException("Queued execution did not complete");
        }
        return frames;
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
