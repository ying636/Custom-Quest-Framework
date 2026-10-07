using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIWorkspaceChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog resources)
    {
        object data = new DialogTreeDef { defName = "CQF_Check_Conversation", label = "CQF_Check_Original" };
        object owner = new(); bool editorValid = true;
        AIFakeLiveMap map = new(model);
        IEnumerable<CQFAITarget> Discover()
        {
            yield return new CQFAITarget(owner, "CQF_Check_Conversation", typeof(DialogTreeDef).FullName!, "editor",
                () => new CQFAIEditorContext("CQF_Check_Conversation", () => data, value => data = value, isValid: () => editorValid, owner: owner), () => editorValid);
            yield return new CQFAITarget(map, "CQF_Check_Map", typeof(CQFAILiveMapInfo).FullName!, "current_map", () => CQFAILiveMapContext.Create(map), () => map.IsValid);
        }
        CQFAITargetCatalog targets = new(Discover);
        CQFAIConversation conversation = new(model, resources);
        CQFAIHarness harness = new(model, resources, conversation, null, "CQF_Check_Command", true, false, true, targets);
        Check(harness.Registry.Tools.Any(tool => tool.Name == "cqf_list_targets") && !harness.Registry.Tools.Any(tool => tool.Name == "cqf_apply_changes"), "workspace starts with discovery and no implicitly bound write target");
        Check(harness.Instructions.Contains("no fixed editing target") && !harness.Transaction!.CanUndo, "workspace instructions require object discovery instead of chat binding");
        harness.Process(AIToolChecks.Response("CQF_Check_Discover", "cqf_list_targets"));
        XElement[] listed = harness.LastResults.Single().Descendants("target").ToArray();
        string editorId = listed.Single(target => target.Attribute("kind")?.Value == "editor").Attribute("id")!.Value;
        string mapId = listed.Single(target => target.Attribute("kind")?.Value == "current_map").Attribute("id")!.Value;
        Check(listed.Length == 2 && targets.Query("Conversation", 0).Elements().Single().Attribute("id")!.Value == editorId, "target discovery filters names and preserves stable IDs within the task");
        harness.Process(AIToolChecks.Response("CQF_Check_SelectEditor", "cqf_select_target", ("target_id", editorId)));
        Check(harness.Registry.Tools.Any(tool => tool.Name == "cqf_add_dialogue_branch") && harness.LastResults.Single().Descendants("tools").Any(), "selecting dialogue data exposes its actual editing tools and XML definitions");
        harness.Process(AIToolChecks.Response("CQF_Check_EditEditor", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_Changed</value></set></changes>")));
        Check(((DialogTreeDef)data).label == "CQF_Check_Changed" && harness.Transaction!.CanUndo, "discovered editor edits apply to the actual host and retain task undo");
        harness.Process(AIToolChecks.Response("CQF_Check_SelectMap", "cqf_select_target", ("target_id", mapId)));
        Check(harness.Registry.Tools.Any(tool => tool.Name == "cqf_read_map_region") && !harness.Registry.Tools.Any(tool => tool.Name == "cqf_add_dialogue_branch")
            && harness.Instructions.Contains("currently running game map"), "switching objects refreshes native tool definitions and live map instructions");
        harness.Process(AIToolChecks.Response("CQF_Check_EditMap", "cqf_apply_changes", ("changes_xml", "<changes><roof def='CQF_Check_Roof' x='2' z='2'/></changes>")));
        Check(map.Cells["roof:2:2"] == "CQF_Check_Roof" && ((DialogTreeDef)data).label == "CQF_Check_Changed", "one request can edit multiple discovered objects without overwriting the previous object");
        harness.Process(AIToolChecks.Response("CQF_Check_ReselectEditor", "cqf_select_target", ("target_id", editorId)));
        harness.Process(AIToolChecks.Response("CQF_Check_EditAgain", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_ChangedAgain</value></set></changes>")));
        Check(harness.Transaction!.IsCurrent && ((DialogTreeDef)data).label == "CQF_Check_ChangedAgain", "reselecting a previously edited object reuses its transaction checkpoint");
        harness.Transaction.Undo();
        Check(((DialogTreeDef)data).label == "CQF_Check_Original" && map.Cells.Count == 0 && !harness.Transaction.CanUndo, "task undo reverses edits across all selected objects");
        CQFAIHarness fresh = new(model, resources, conversation, null, "", true, false, false, new CQFAITargetCatalog(Discover));
        fresh.Process(AIToolChecks.Response("CQF_Check_NoHistoricalID", "cqf_select_target", ("target_id", mapId)));
        Check(fresh.LastResults.Single().Element("error")?.Attribute("code")?.Value == "CQF_AI_MissingTarget", "a new request rejects historical target IDs and requires fresh discovery");
        fresh.Process(AIToolChecks.Response("CQF_Check_NewList", "cqf_list_targets"));
        string newEditor = fresh.LastResults.Single().Descendants("target").Single(target => target.Attribute("kind")!.Value == "editor").Attribute("id")!.Value;
        fresh.Process(AIToolChecks.Response("CQF_Check_NewSelect", "cqf_select_target", ("target_id", newEditor)));
        editorValid = false;
        fresh.Process(AIToolChecks.Response("CQF_Check_ClosedRead", "cqf_read_target"));
        Check(fresh.LastResults.Single().Element("error")?.Attribute("code")?.Value == "CQF_AI_MissingTarget", "closed objects return recoverable errors instead of blocking discovery");
        fresh.Process(AIToolChecks.Response("CQF_Check_AfterClose", "cqf_list_targets"));
        Check(fresh.LastResults.Single().Descendants("target").All(target => target.Attribute("kind")!.Value != "editor"), "object discovery remains available after the selected editor closes");
        string newMap = fresh.LastResults.Single().Descendants("target").Single().Attribute("id")!.Value;
        fresh.Process(AIToolChecks.Response("CQF_Check_RecoverMap", "cqf_select_target", ("target_id", newMap)));
        Check(fresh.Registry.Tools.Any(tool => tool.Name == "cqf_read_map_region"), "the same request can recover by selecting another available object");
        map.IsValid = false;
        Reject(() => targets.Resolve(mapId), "removed maps cannot be selected through cached target IDs"); map.IsValid = true;
        CQFAIHarness readonlyHarness = new(model, resources, new CQFAIConversation(model, resources), null, "", false, false, true, new CQFAITargetCatalog(Discover));
        readonlyHarness.Process(AIToolChecks.Response("CQF_Check_ReadonlyList", "cqf_list_targets"));
        string readonlyId = readonlyHarness.LastResults.Single().Descendants("target").Single().Attribute("id")!.Value;
        readonlyHarness.Process(AIToolChecks.Response("CQF_Check_ReadonlySelect", "cqf_select_target", ("target_id", readonlyId)));
        Check(readonlyHarness.Transaction == null && readonlyHarness.Registry.Tools.Any(tool => tool.Name == "cqf_read_map_region") && !readonlyHarness.Registry.Tools.Any(tool => tool.Name == "cqf_apply_changes"),
            "object selection retains inspection while respecting disabled editing permission");
        readonlyHarness.Process(AIToolChecks.Response("CQF_Check_ReadonlyWrite", "cqf_apply_changes", ("changes_xml", "<changes/>")));
        Check(readonlyHarness.LastResults.Single().Element("error")?.Attribute("code")?.Value == "CQF_AI_ReadOnly", "discovery cannot bypass disabled write permissions");
        Reject(() => targets.Query("", -1), "target pagination validates offsets");
        Reject(() => targets.Query(new string('x', 101), 0), "target searches remain bounded");
        editorValid = true;
        CQFAIHarness conflict = new(model, resources, new CQFAIConversation(model, resources), null, "", true, false, true, new CQFAITargetCatalog(Discover));
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictList", "cqf_list_targets"));
        string conflictId = conflict.LastResults.Single().Descendants("target").Single(target => target.Attribute("kind")!.Value == "editor").Attribute("id")!.Value;
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictSelect", "cqf_select_target", ("target_id", conflictId)));
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictEdit", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_BeforeManual</value></set></changes>")));
        ((DialogTreeDef)data).label = "CQF_Check_Manual";
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictReject", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_StaleOverwrite</value></set></changes>")));
        Check(((DialogTreeDef)data).label == "CQF_Check_Manual" && conflict.LastResults.Single().Element("error") != null, "manual edits prevent a selected stale draft from being overwritten");
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictRefresh", "cqf_select_target", ("target_id", conflictId)));
        conflict.Process(AIToolChecks.Response("CQF_Check_ConflictContinue", "cqf_apply_changes", ("changes_xml", "<changes><set path='/description'><value>CQF_Check_NewDescription</value></set></changes>")));
        Check(((DialogTreeDef)data).label == "CQF_Check_Manual" && ((DialogTreeDef)data).description == "CQF_Check_NewDescription" && !conflict.Transaction!.IsCurrent,
            "reselecting after a conflict reads current data and permits new edits while keeping unsafe task undo disabled");
    }
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
