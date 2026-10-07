using System.Reflection;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AILoadedTargetChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog resources)
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CQF_AI_DefinitionChecks_" + Guid.NewGuid().ToString("N")));
        FieldInfo rootField = typeof(CQFContentPaths).GetField("root", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? originalRoot = rootField.GetValue(null);
        try
        {
            Directory.CreateDirectory(root); CQFContentPaths.Initialize(root);
            DialogTreeDef source = new() { defName = "CQF_Check_RuntimeDefinition", title = "CQF_Check_首次通讯", label = "CQF_Check_LoadedOriginal", modContentPack = LoadedModManager.RunningModsListForReading.First() };
            DefDatabase<DialogTreeDef>.Add(source);
            IEnumerable<CQFAITarget> Discover() => CQFAITargetCatalog.Discover(model, Array.Empty<CQFAIEditorContext>(), Array.Empty<Map>());
            CQFAITargetCatalog targets = new(Discover);
            XElement listing = targets.Query(source.defName, 0);
            Check(listing.Elements("target").Single().Attribute("kind")!.Value == "loaded_definition", "loaded CQF data is discoverable without an open editor or game map");
            string id = listing.Elements("target").Single().Attribute("id")!.Value;
            Check(targets.Query("首次通讯", 0).Elements("target").Single().Attribute("id")!.Value == id, "object discovery can find dialogue data by its displayed title");
            CQFAIHarness harness = new(model, resources, new CQFAIConversation(model, resources), null, "", true, false, true, targets);
            harness.Process(AIToolChecks.Response("CQF_Check_LoadedSelect", "cqf_select_target", ("target_id", id)));
            int errorsBefore = ChecksLogHandler.ErrorCount;
            harness.Process(AIToolChecks.Response("CQF_Check_LoadedEdit", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_LoadedChanged</value></set></changes>")));
            Check(DefDatabase<DialogTreeDef>.GetNamed(source.defName).label == "CQF_Check_LoadedChanged" && harness.LastResults.Single().Attribute("success")!.Value == "true",
                "selected loaded definitions use the existing framework hot-load API and change the actual Def database");
            Check(ChecksLogHandler.ErrorCount == errorsBefore, "valid runtime definition edits introduce no framework validation errors");
            Check(targets.Query("CQF_Check_LoadedChanged", 0).Elements("target").Single().Attribute("id")!.Value == id, "label edits preserve runtime target identity and update discovery labels");
            harness.Process(AIToolChecks.Response("CQF_Check_LoadedReselect", "cqf_select_target", ("target_id", id)));
            harness.Process(AIToolChecks.Response("CQF_Check_LoadedSecondEdit", "cqf_apply_changes", ("changes_xml", "<changes><set path='/description'><value>CQF_Check_LoadedDescription</value></set></changes>")));
            Check(harness.Transaction!.IsCurrent && DefDatabase<DialogTreeDef>.GetNamed(source.defName).description == "CQF_Check_LoadedDescription", "reselecting a changed loaded definition reuses its undo transaction");
            harness.Process(AIToolChecks.Response("CQF_Check_LoadedRename", "cqf_apply_changes", ("changes_xml", "<changes><set path='/defName'><value>CQF_Check_UnsupportedRename</value></set></changes>")));
            Check(harness.LastResults.Single().Element("error")?.Attribute("code")?.Value == "CQF_AI_DefinitionNameFixed" && DefDatabase<DialogTreeDef>.GetNamedSilentFail("CQF_Check_UnsupportedRename") == null,
                "runtime definition renaming cannot create duplicate database entries");
            harness.Transaction.Undo();
            Check(DefDatabase<DialogTreeDef>.GetNamed(source.defName).label == "CQF_Check_LoadedOriginal", "task undo restores the prior loaded definition through the framework API");
            Check(!Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories).Any(), "runtime definition tools never claim or perform source XML persistence");
            CQFAIEditorContext editor = new(source.defName, () => source, _ => { }, owner: new object());
            CQFAITarget[] deduplicated = CQFAITargetCatalog.Discover(model, new[] { editor }, Array.Empty<Map>()).Where(target => target.Name == source.defName).ToArray();
            Check(deduplicated.Length == 1 && deduplicated[0].Kind == "editor", "an open editor takes precedence over the corresponding loaded resource");
        }
        finally
        {
            rootField.SetValue(null, originalRoot);
            if (Directory.Exists(root) && Path.GetFileName(root).StartsWith("CQF_AI_DefinitionChecks_", StringComparison.Ordinal)
                && Path.GetDirectoryName(root) == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)) Directory.Delete(root, true);
        }
    }
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
}
