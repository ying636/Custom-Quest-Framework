using System.Xml.Linq;
using QuestEditor_Library;

internal static class AILiveMapChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        AIFakeLiveMap map = new(model);
        CQFAIEditorContext context = CQFAILiveMapContext.Create(map);
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness harness = new(model, catalog, conversation, context, "CQF_Check_Command", true, false);
        Check(harness.Transaction is CQFAILiveMapTransaction && harness.Registry.Tools.Any(tool => tool.Name == "cqf_edit_map_thing"), "current map uses a live transaction and actual Thing editing tools");
        Check(harness.Instructions.Contains("currently running game map") && harness.Instructions.Contains("All placement, terrain, roof and erase operations apply immediately"), "live map instructions describe immediate actual map edits");
        XElement capabilities = harness.Registry.Tools.Single(tool => tool.Name == "cqf_get_context").Execute(new XElement("arguments")).Element("capabilities")!;
        Check(capabilities.Attribute("liveMapConstruction")?.Value == "true" && capabilities.Attribute("canEditTarget")?.Value == "true", "map context advertises construction permission and capability");
        Check(!model.Write(map.Info, root: true).Elements("backend").Any() && ReferenceEquals(((CQFAILiveMapInfo)model.Copy(map.Info)).backend, map), "map runtime backend is preserved internally and excluded from model data");
        Check(harness.Process(AIToolChecks.Response("live_place", "cqf_apply_changes", ("changes_xml", "<changes><place def='CQF_Check_Terminal' x='30' z='30'/><place def='CQF_Check_Entrance' x='31' z='30'/><place def='CQF_Check_Exit' x='32' z='30'/></changes>"))) && map.Things.Count == 3,
            "live tool calls immediately change backend objects before final reply");
        Check(harness.LastResults.Single().Descendants("placed").Count() == 3 && harness.LastResults.Single().Descendants("liveMapApplied").Any(), "live receipts expose actual placed object IDs");
        string interactive = map.Things.Single(pair => pair.Value.interaction != null).Key;
        string entrance = map.Things.Single(pair => pair.Value.entrance != null).Key;
        string exit = map.Things.Single(pair => pair.Value.exit != null).Key;
        harness.Process(AIToolChecks.Response("live_interaction", "cqf_add_interaction", ("thing_id", interactive), ("operation_xml", "<value Class='QuestEditor_Library.InteractionOperation'><interactionText>CQF_Check_LiveOption</interactionText></value>")));
        Check(map.Things[interactive].interaction!.operations.Single().interactionText == "CQF_Check_LiveOption", "semantic interaction tools edit a live Thing by actual ID");
        harness.Process(AIToolChecks.Response("live_entrance", "cqf_configure_entrance", ("thing_id", entrance), ("configuration_xml", "<value><exitName>CQF_Check_LiveExit</exitName><opended>false</opended></value>")));
        harness.Process(AIToolChecks.Response("live_exit", "cqf_configure_exit", ("thing_id", exit), ("configuration_xml", "<value><exitName>CQF_Check_LiveExit</exitName></value>")));
        Check(!map.Things[entrance].entrance!.opended && map.Things[exit].exit!.exitName == "CQF_Check_LiveExit", "entrance and exit tools edit actual live configurations");
        harness.Process(AIToolChecks.Response("live_health", "cqf_edit_map_thing", ("thing_id", interactive), ("changes_xml", "<changes><set path='/hitPoints'><value>80</value></set></changes>")));
        Check(map.Things[interactive].hitPoints == 80 && harness.Transaction!.IsCurrent, "generic live Thing edits read back actual data across tool rounds");
        harness.Process(AIToolChecks.Response("live_read", "cqf_read_map_thing", ("thing_id", interactive)));
        Check(harness.LastResults.Single().Descendants("hitPoints").Single().Value == "80", "Thing read tool returns the current edited state");
        harness.Process(AIToolChecks.Response("live_region", "cqf_read_map_region", ("x", "24"), ("z", "24"), ("width", "16"), ("height", "16"), ("limit", "10")));
        Check(harness.LastResults.Single().Descendants("cell").Count() == 10, "live region reads use bounded pagination");
        Check(!harness.Process("<assistant><reply>CQF_Check_LiveDone</reply></assistant>"), "live editing task can finish after actual readback");
        harness.Transaction!.Undo();
        Check(map.Things.Count == 0 && !harness.Transaction.CanUndo, "one task undo reverses construction and all later object configuration edits");
        CQFAIHarness terrain = new(model, catalog, new CQFAIConversation(model, catalog), context, "", true, false);
        terrain.Process(AIToolChecks.Response("floor_one", "cqf_apply_changes", ("changes_xml", "<changes><terrain def='CQF_Check_First' x='1' z='1'/></changes>")));
        terrain.Process(AIToolChecks.Response("floor_two", "cqf_apply_changes", ("changes_xml", "<changes><terrain def='CQF_Check_Second' x='1' z='1'/><roof def='CQF_Check_Roof' x='1' z='1'/></changes>")));
        Check(terrain.Transaction!.IsCurrent && map.Cells["terrain:1:1"] == "CQF_Check_Second", "repeated edits of one cell maintain a current undo checkpoint");
        terrain.Transaction.Undo(); Check(map.Cells.Count == 0, "repeated terrain and roofing edits restore the initial cell state");
        CQFAIHarness readonlyMap = new(model, catalog, new CQFAIConversation(model, catalog), context, "", false, false);
        Check(readonlyMap.Registry.Tools.Any(tool => tool.Name == "cqf_read_map_region") && !readonlyMap.Registry.Tools.Any(tool => tool.Name == "cqf_edit_map_thing"), "disabled permission retains map inspection and removes all map write tools");
        readonlyMap.Process(AIToolChecks.Response("readonly_live", "cqf_edit_map_thing", ("thing_id", interactive), ("changes_xml", "<changes/>")));
        Check(readonlyMap.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_ReadOnly", "live Thing writes cannot bypass disabled editing permission");
        CQFAIHarness recovering = new(model, catalog, new CQFAIConversation(model, catalog), context, "", true, false);
        map.FailKey = "roof:3:3";
        recovering.Process(AIToolChecks.Response("failed_live_batch", "cqf_apply_changes", ("changes_xml", "<changes><terrain def='CQF_Check_Floor' x='3' z='3'/><roof def='CQF_Check_Roof' x='3' z='3'/></changes>")));
        Check(map.Cells.Count == 0 && !recovering.Transaction!.CanUndo, "partial live batch failure rolls back every already applied operation");
        map.FailKey = null;
        recovering.Process(AIToolChecks.Response("corrected_live_batch", "cqf_apply_changes", ("changes_xml", "<changes><terrain def='CQF_Check_Floor' x='3' z='3'/></changes>")));
        Check(map.Cells.Count == 1 && recovering.Transaction!.CanUndo, "model can correct a failed live edit and continue the same task");
        map.Cells["terrain:3:3"] = "CQF_Check_PlayerChange";
        Reject(recovering.Transaction!.Undo, "manual map modifications prevent stale undo from overwriting player changes");
        Check(map.Cells["terrain:3:3"] == "CQF_Check_PlayerChange", "rejected undo preserves newer map changes");
        CQFAIHarness stale = new(model, catalog, new CQFAIConversation(model, catalog), context, "", true, false);
        map.IsValid = false;
        Reject(() => stale.Process(AIToolChecks.Response("map_switch", "cqf_validate_target")), "changing maps invalidates outstanding live tasks");
        map.IsValid = true;
        CQFAIHarness invalid = new(model, catalog, new CQFAIConversation(model, catalog), context, "", true, false);
        invalid.Process(AIToolChecks.Response("live_metadata", "cqf_apply_changes", ("changes_xml", "<changes><set path='/size'><value>10,1,10</value></set></changes>")));
        Check(invalid.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_LiveMapOperationRequired", "current map metadata cannot be edited as if it were a saved data draft");
        invalid.Process(AIToolChecks.Response("bad_region", "cqf_read_map_region", ("x", "63"), ("z", "63"), ("width", "2"), ("height", "2")));
        Check(invalid.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_MapBounds", "out-of-bounds live reads are rejected before backend execution");
        var backendFailure = new AIFakeLiveMap(model) { FailKey = "roof:4:4", FailUndo = true };
        CQFAIHarness rollback = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(backendFailure), "", true, false);
        try { rollback.Process(AIToolChecks.Response("rollback_failure", "cqf_apply_changes", ("changes_xml", "<changes><roof def='CQF_Check_Roof' x='4' z='4'/></changes>"))); }
        catch (AggregateException error)
        {
            Check(error.InnerExceptions.Count == 2, "live rollback failure preserves both the original operation error and restore error");
            Check(rollback.Transaction!.CanUndo && rollback.Transaction.IsCurrent, "incomplete rollback retains the affected live edits for another undo attempt");
            backendFailure.FailUndo = false;
            rollback.Transaction.Undo(); Check(backendFailure.Cells.Count == 0, "remaining live edits can be restored after the rollback cause is resolved");
            AILiveMapPlanChecks.Run(model);
            return;
        }
        throw new InvalidOperationException("Expected visible rollback failure");
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
