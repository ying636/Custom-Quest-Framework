using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIToolChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        foreach (var pair in new[] { ("CQF_Check_Terminal", typeof(InteractableThing)), ("CQF_Check_Entrance", typeof(CustomMapEntrance)), ("CQF_Check_Exit", typeof(CustomMapExit)) })
        {
            ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = pair.Item1; def.label = pair.Item1; def.thingClass = pair.Item2; def.size = new IntVec2(1, 1); def.stackLimit = 1; def.category = ThingCategory.Building;
            def.modContentPack = LoadedModManager.RunningModsListForReading.First();
            DefDatabase<ThingDef>.Add(def);
        }
        CustomMapDataDef destination = AICheckFixtures.Map(); destination.defName = "CQF_Check_Destination";
        destination.customThings.Add(new CustomThingData_CustomMapExit { def = DefDatabase<ThingDef>.GetNamed("CQF_Check_Exit"), position = new IntVec3(4, 0, 4), exitName = "CQF_Check_Return" });
        DefDatabase<CustomMapDataDef>.Add(destination);
        object current = AICheckFixtures.Map();
        string original = model.Write(current, root: true).ToString();
        CQFAIEditorContext context = new CQFAIEditorContext("CQF_Check_Tools", () => current, value => current = value);
        CQFAIConversation conversation = new CQFAIConversation(model, catalog);
        CQFAIHarness harness = new CQFAIHarness(model, catalog, conversation, context, "CQF_Check_Command", true, false);
        Check(harness.Registry.Tools.Any(tool => tool.Name == "cqf_add_interaction") && harness.Registry.Tools.Any(tool => tool.Name == "cqf_configure_entrance"), "map registry exposes semantic interaction and portal tools");
        Check(harness.Registry.Tools.Single(tool => tool.Name == "cqf_get_schema").Execute(new XElement("arguments", new XElement("type", typeof(InteractionOperation).FullName))).Elements("type").Count() == 1, "tool schema loads one type on demand");
        Check(harness.Registry.Tools.Single(tool => tool.Name == "cqf_find_types").Execute(new XElement("arguments", new XElement("base_type", typeof(CustomThingData).FullName)))
            .Elements("type").Any(type => type.Attribute("name")?.Value == typeof(CustomThingData_CustomMapEntrance).FullName), "type discovery exposes concrete portal data types");
        string place = "<changes><place def='CQF_Check_Terminal' Class='QuestEditor_Library.CustomThingData_InteractableThing' x='4' z='4'/><place def='CQF_Check_Entrance' Class='QuestEditor_Library.CustomThingData_CustomMapEntrance' x='8' z='8'/><place def='CQF_Check_Exit' Class='QuestEditor_Library.CustomThingData_CustomMapExit' x='12' z='12'/></changes>";
        Check(harness.Process(Response("place", "cqf_apply_changes", ("changes_xml", place))) && ((CustomMapDataDef)current).customThings.Count == 3, "tool writes become visible immediately and request continues");
        string operation = "<value Class='QuestEditor_Library.InteractionOperation'><interactionText>CQF_Check_Open</interactionText><tickToOperate>120</tickToOperate><results><li Class='QuestEditor_Library.InteractionResult'><actions><li Class='QuestEditor_Library.CQFAction_SentSignal'><signal>CQF_Check_OpenSignal</signal></li></actions></li></results></value>";
        harness.Process(Response("interaction", "cqf_add_interaction", ("object_path", "/customThings/0"), ("operation_xml", operation)));
        Check(((CustomThingData_InteractableThing)((CustomMapDataDef)current).customThings[0]).operations.Single().results.Single().actions.Single() is CQFAction_SentSignal signal && signal.signal == "CQF_Check_OpenSignal", "semantic interaction tool preserves nested result actions");
        harness.Process(Response("entrance", "cqf_configure_entrance", ("object_path", "/customThings/1"), ("configuration_xml", "<value><data>CQF_Check_Destination</data><exitName>CQF_Check_Return</exitName><opended>false</opended></value>")));
        harness.Process(Response("exit", "cqf_configure_exit", ("object_path", "/customThings/2"), ("configuration_xml", "<value><exitName>CQF_Check_Return</exitName></value>")));
        Check(!((CustomThingData_CustomMapEntrance)((CustomMapDataDef)current).customThings[1]).opended, "semantic entrance tool configures actual opened state");
        harness.Process(Response("validate", "cqf_validate_target"));
        Check(harness.LastResults.Single().Descendants("validation").Single().Attribute("warnings")?.Value == "0", "portal validation finds matching exit in actual destination Def");
        Check(!harness.Process("<assistant><reply>CQF_Check_Done</reply></assistant>"), "task finishes after tool results are checked");
        harness.Transaction!.Undo();
        Check(model.Write(current, root: true).ToString() == original, "one undo restores all task changes across multiple tool rounds");
        Check(new CQFAITargetReader(model).Read(destination, "/customThings", 0, 1).Attribute("total")?.Value == "1", "target reader paginates collections");
        Reject(() => new CQFAITargetReader(model).Read(destination, "/customThings", -1, 1), "negative page offsets rejected");
        Check(harness.Registry.Tools.Single(tool => tool.Name == "cqf_read_resource").Execute(new XElement("arguments", new XElement("type", typeof(CustomMapDataDef).FullName),
            new XElement("name", destination.defName), new XElement("path", "/customThings"))).Descendants("exitName").Single().Value == "CQF_Check_Return", "related map resource data is read on demand without changing the edit target");
        CQFAIHarness recovering = new CQFAIHarness(model, catalog, conversation, context, "", true, false);
        recovering.Process(Response("bad_path", "cqf_apply_changes", ("changes_xml", "<changes><set path='/not_a_field'><value>1</value></set></changes>")));
        Check(recovering.LastResults.Single().Attribute("success")?.Value == "false" && conversation.Messages.Last().Role == "tool", "invalid edit returns structured tool feedback");
        recovering.Process(Response("bad_number", "cqf_apply_changes", ("changes_xml", "<changes><mapResize x='large' z='12'/></changes>")));
        Check(recovering.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_InvalidValue", "invalid map numbers are recoverable tool errors");
        recovering.Process(Response("corrected", "cqf_apply_changes", ("changes_xml", "<changes><mapResize x='16' z='16'/></changes>")));
        Check(((CustomMapDataDef)current).size.x == 16, "model can correct a failed edit within the same task");
        ((CustomMapDataDef)current).label = "CQF_Check_Manual";
        Check(recovering.Process(Response("changed_read", "cqf_read_target", ("path", "/label"))) && recovering.LastResults.Single().Value.Contains("CQF_Check_Manual"), "manual editor changes remain readable without invalidating the target");
        Reject(() => recovering.Process(Response("stale", "cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_Overwrite</value></set></changes>"))), "manual editor changes still prevent stale draft writes");
        CQFAIHarness readOnly = new CQFAIHarness(model, catalog, conversation, context, "", false, false);
        Check(readOnly.Instructions.Contains("Editing permission is disabled"), "disabled editing permission is reported explicitly");
        CQFAIHarness noTarget = new CQFAIHarness(model, catalog, new CQFAIConversation(model, catalog), null, "", true, false);
        Check(noTarget.Instructions.Contains("Editing permission is enabled, but no supported editing target") && !noTarget.Instructions.Contains("Read-only chat/resource mode"), "missing target is distinguished from disabled editing permission");
        XElement capabilities = noTarget.Registry.Tools.Single(tool => tool.Name == "cqf_get_context").Execute(new XElement("arguments")).Element("capabilities")!;
        Check(capabilities.Attribute("editingAllowed")?.Value == "true" && capabilities.Attribute("hasTarget")?.Value == "false" && capabilities.Attribute("unavailableReason")?.Value == "no_target", "context tool reports permission and target availability separately");
        Check(capabilities.Attribute("liveMapConstruction")?.Value == "false" && noTarget.Instructions.Contains("Live construction is available when the active target is the current game map"), "missing target does not claim live map construction capability");
        noTarget.Process(Response("no_target_write", "cqf_apply_changes", ("changes_xml", "<changes/>")));
        Check(noTarget.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_NoEditingTarget", "write attempts without a target report missing target instead of disabled permission");
        Check(!readOnly.Registry.Tools.Any(tool => tool.Name == "cqf_apply_changes"), "read-only mode does not register write tools");
        readOnly.Process(Response("read_only", "cqf_apply_changes", ("changes_xml", "<changes/>")));
        Check(readOnly.LastResults.Single().Attribute("success")?.Value == "false", "invented write calls cannot bypass read-only mode");
        CQFAIToolCall malformed = CQFAIToolCall.FromJson(new XElement("item", new XElement("type", "function"), new XElement("id", "bad_json"),
            new XElement("function", new XElement("name", "cqf_get_schema"), new XElement("arguments", "{invalid json"))));
        Check(readOnly.Process(new XElement("assistant", new XElement("tools", malformed.ToXml())).ToString()) && readOnly.LastResults.Single().Attribute("success")?.Value == "false",
            "malformed native argument JSON is reported through tool feedback instead of ending the task");
        Check(CQFAIToolCall.FromXml(malformed.ToXml()).ToJson().Element("function")!.Element("arguments")!.Value == "{invalid json", "native call transcript preserves original malformed arguments for recovery");
        CQFAIToolCall wrongType = CQFAIToolCall.FromJson(new XElement("item", new XElement("type", "function"), new XElement("id", "wrong_type"),
            new XElement("function", new XElement("name", "cqf_get_schema"), new XElement("arguments", "{\"type\":123}"))));
        readOnly.Process(new XElement("assistant", new XElement("tools", wrongType.ToXml())).ToString());
        Check(readOnly.LastResults.Single().Descendants("error").Single().Attribute("code")?.Value == "CQF_AI_InvalidTool", "native argument type violations are recoverable tool errors");
        readOnly.Process(Response("schema_fixed", "cqf_get_schema", ("type", typeof(InteractionOperation).FullName!)));
        Check(readOnly.LastResults.Single().Attribute("success")?.Value == "true", "corrected native arguments execute successfully after failed calls");
        current = new DialogTreeDef { defName = "CQF_Check_ToolDialogue" };
        CQFAIHarness dialogue = new CQFAIHarness(model, catalog, conversation, context, "", true, false);
        dialogue.Process(Response("branch", "cqf_add_dialogue_branch", ("source_node_id", "0"), ("node_xml", "<value><text>CQF_Check_BranchText</text></value>"), ("option_xml", "<value><text>CQF_Check_BranchChoice</text></value>")));
        DialogTreeDef tree = (DialogTreeDef)current;
        Check(tree.nodeMoulds.Count == 2 && tree.nodeMoulds[0].options.Single().results.Single().nextIndex == 1 && tree.curIndex == 2, "dialogue branch tool allocates and links a new node atomically");
        for (int i = 2; i < 100; i++) tree.nodeMoulds.Add(i, new DialogNode { index = i, text = "CQF_Check_Huge_" + i + new string('x', 700) });
        CQFAIHarness compact = new CQFAIHarness(model, catalog, conversation, context, "", true, false);
        Check(compact.Instructions.Length < 30000 && !compact.Instructions.Contains("CQF_Check_Huge_99"), "initial context omits full node data and recursive schemas");
        Console.WriteLine("Harness initial context characters: " + compact.Instructions.Length);
        string nativeInstructions = conversation.Instructions(tree, true, false, compact.Registry, true);
        Check(nativeInstructions.Length < compact.Instructions.Length && !nativeInstructions.Contains("CQF_Check_Huge_99"), "native large-dialogue context omits duplicated definitions and full node text");
        Console.WriteLine("Harness native initial context characters: " + nativeInstructions.Length);
        Action<CQFAIToolRegistry, CQFAIEditorContext?> extension = (registry, _) => registry.Register(new CQFAITool("cqf_check_extension", "CQF_Check_Extension", _ => new XElement("extension")));
        CQFAIToolRegistry.RegisterTools += extension;
        try { Check(new CQFAIHarness(model, catalog, conversation, context, "", false, false).Registry.Tools.Any(tool => tool.Name == "cqf_check_extension"), "loaded extensions can register tools without changing harness code"); }
        finally { CQFAIToolRegistry.RegisterTools -= extension; }
        LiveChecks(model);
        object ignored = new DialogTreeDef { defName = "CQF_Check_Ignored" };
        CQFAITransaction mismatch = new CQFAITransaction(model, new CQFAIEditorContext("CQF_Check_Ignored", () => ignored, _ => { }));
        mismatch.Build(XElement.Parse("<changes><set path='/label'><value>CQF_Check_Changed</value></set></changes>"), "", false);
        Reject(mismatch.Apply, "post-apply readback rejects an editor that did not apply the requested data");
        Check(!mismatch.CanUndo && mismatch.IsCurrent, "readback failure leaves the target unchanged");
        CQFAIConversation legacyConversation = new CQFAIConversation(model, catalog);
        CQFAIHarness legacy = new CQFAIHarness(model, catalog, legacyConversation, context, "", true, false);
        Check(legacy.Process("<assistant><changes><set path='/label'><value>CQF_Check_Legacy</value></set></changes></assistant>"), "legacy XML edits also enter the result feedback loop");
        legacyConversation.BeginTask();
        Check(!legacyConversation.RequestMessages.Any(message => message.Role == "tool" || message.ToolCalls.Count > 0), "new tasks exclude old tool transcripts and unfinished tool batches");
    }
    public static string Response(string id, string name, params (string name, string value)[] arguments) => new XElement("assistant", new XElement("tools",
        new CQFAIToolCall(id, name, new XElement("arguments", arguments.Select(argument => new XElement(argument.name, argument.value)))).ToXml())).ToString(SaveOptions.DisableFormatting);
    private static void LiveChecks(CQFAIModel model)
    {
        InteractableThing thing = NewThing<InteractableThing>();
        thing.operations = new List<InteractionOperation> { new InteractionOperation { interactionText = "CQF_Check_Original" } };
        thing.operationDefs = new List<InteractionDataDef>();
        CQFAIEditorContext live = CQFAIThingContext.Create(thing)!;
        CQFAITransaction interaction = new CQFAITransaction(model, live);
        Check(interaction.IsCurrent && !ReferenceEquals(live.Read(), live.Read()), "live snapshots have stable target identity despite fresh configuration objects");
        interaction.Build(XElement.Parse("<changes><set path='/operations/0/tickToOperate'><value>200</value></set></changes>"), "", false);
        interaction.Apply();
        Check(thing.operations.Single().tickToOperate == 200 && interaction.IsCurrent, "live interaction changes are written back to the actual Thing");
        interaction.Undo(); Check(thing.operations.Single().tickToOperate == 100, "live interaction undo restores actual Thing configuration");
        CustomMapEntrance entrance = NewThing<CustomMapEntrance>(); entrance.enterActions = new List<CQFAction>(); entrance.opended = true; entrance.exitName = "CQF_Check_Return";
        CQFAITransaction entry = new CQFAITransaction(model, CQFAIThingContext.Create(entrance)!);
        entry.Build(XElement.Parse("<changes><set path='/opended'><value>false</value></set><set path='/data'><value>CQF_Check_Destination</value></set></changes>"), "", false);
        entry.Apply(); Check(!entrance.opended && entrance.MapDef?.defName == "CQF_Check_Destination", "live entrance destination and opened state are editable");
        entry.Undo(); Check(entrance.opended && entrance.MapDef == null, "live entrance undo restores original destination and state");
        CustomMapExit exit = NewThing<CustomMapExit>(); exit.enterActions = new List<CQFAction>(); exit.exitName = "CQF_Check_OldExit";
        CQFAITransaction departure = new CQFAITransaction(model, CQFAIThingContext.Create(exit)!);
        departure.Build(XElement.Parse("<changes><set path='/exitName'><value>CQF_Check_NewExit</value></set></changes>"), "", false);
        departure.Apply(); Check(exit.exitName == "CQF_Check_NewExit", "live exit edits reach the actual exit object");
        departure.Undo(); Check(exit.exitName == "CQF_Check_OldExit", "live exit undo restores its identifier");
        entrance.exit = exit;
        CQFAITransaction linked = new CQFAITransaction(model, CQFAIThingContext.Create(entrance)!);
        Reject(() => linked.Build(XElement.Parse("<changes><set path='/exitName'><value>CQF_Check_BrokenLink</value></set></changes>"), "", false), "linked runtime portal identifiers cannot be changed independently");
        CustomMapEntrance_Chance chance = NewThing<CustomMapEntrance_Chance>(); chance.enterActions = new List<CQFAction>(); chance.tagWithChance = new List<TagWithChance>(); chance.mapDefWithChance = new List<MapDefWithChance>();
        CQFAITransaction random = new CQFAITransaction(model, CQFAIThingContext.Create(chance)!);
        random.Build(XElement.Parse("<changes><append path='/mapDefWithChance'><value Class='QuestEditor_Library.MapDefWithChance'><def>CQF_Check_Destination</def><chance>1</chance></value></append></changes>"), "", false);
        random.Apply(); Check(chance.mapDefWithChance.Single().def.defName == "CQF_Check_Destination" && chance.MapDef == null, "chance entrance editing does not reroll or generate a map");
        random.Undo(); Check(chance.mapDefWithChance.Count == 0, "chance entrance undo restores its pool");
        Check(!model.Types.Contains(typeof(InteractableThing)) && model.Types.Contains(typeof(CQFAIInteractableConfiguration)), "runtime Things stay outside generic data serialization");
    }
    private static T NewThing<T>() where T : Thing
    {
        T thing = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(thing, (sbyte)-1);
        return thing;
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
