using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

internal static class Program
{
    private static void Main()
    {
        string game = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", ".."));
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string path = Path.Combine(game, "RimWorldWin64_Data", "Managed", new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(game); } catch (Exception error) { Console.WriteLine(error); Environment.Exit(1); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string game)
    {
        UnityEngine.Debug.unityLogger.logHandler = new ChecksLogHandler();
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        LanguageDatabase.activeLanguage = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        LanguageDatabase.activeLanguage.info = new LanguageInfo();
        typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(LanguageDatabase.activeLanguage, true);
        FieldInfo replacements = typeof(LoadedLanguage).GetField("keyedReplacements", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)!;
        replacements.SetValue(LanguageDatabase.activeLanguage, Activator.CreateInstance(replacements.FieldType));
        LanguageDatabase.defaultLanguage = LanguageDatabase.activeLanguage;
        ModContentPack mod = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        mod.assemblies = new ModAssemblyHandler(mod);
        mod.assemblies.loadedAssemblies.Add(typeof(DialogTreeDef).Assembly);
        mod.assemblies.loadedAssemblies.Add(typeof(CQFAIWindow).Assembly);
        typeof(ModContentPack).GetField("packageIdInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mod, "CQF.Checks");
        typeof(ModContentPack).GetField("nameInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mod, "CQF_Checks");
        LoadedModManager.RunningModsListForReading.Add(mod);
        GenTypes.ClearCache();
        Check(typeof(CQFAIWindow).Assembly == typeof(QuestEditor_Dialog).Assembly, "AI window belongs to editor assembly");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.AI"), "AI has no separate assembly");
        Check(!CQFAIBridge.IsLoaded && !new CustomQuestFramework_ModSetting().enableEditor, "AI entry is unavailable before editor initialization");
        Check(!CustomQuestFramework_ModSetting.setting.dialogAIEnabled, "network requests default off");
        Check(CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing, "editing preference defaults on independently of the active editor");
        Check(CustomQuestFramework_ModSetting.setting.dialogAIAllowTextGeneration, "new prose generation defaults on");
        AISettingsChecks.Run();
        CQFAIModel model = new CQFAIModel();
        CQFAIChanges changes = new CQFAIChanges(model);
        AIMapChecks.Run(model);
        ThinkNode_Priority think = new ThinkNode_Priority();
        ThinkNode_Priority thinkDraft = (ThinkNode_Priority)changes.Build(think, XElement.Parse("<changes><set path='/priority'><value>0.75</value></set></changes>"), "", false);
        Check(thinkDraft.GetPriority(null!) == 0.75f && !model.Fields(think.GetType()).Any(field => field.Name == "parent"), "protected ThinkNode configuration is editable and runtime parent excluded");
        Check(thinkDraft.TryGetPriority(null!, out float configuredPriority) && configuredPriority == 0.75f, "edited ThinkNode priority is visible to runtime sorter");
        ThinkNode_Priority parent = new ThinkNode_Priority { subNodes = new List<ThinkNode> { new ThinkNode_Priority() } };
        parent.subNodes[0].parent = parent;
        ThinkNode_Priority parentDraft = (ThinkNode_Priority)model.Copy(parent);
        Check(parentDraft.subNodes.Single().parent == parentDraft && parent.subNodes.Single().parent == parent, "copied ThinkNode children bind to their own tree");
        Reject(() => model.Read(XElement.Parse("<value null='true'><field/></value>"), typeof(DialogNode)), "null cannot silently discard supplied fields");
        LootData loot = new LootData();
        Reject(() => changes.Build(loot, XElement.Parse("<changes><set path='/message'><value>Unsupplied narrative</value></set></changes>"), "", false), "loot messages require supplied text or generation permission");
        Check(((LootData)changes.Build(loot, XElement.Parse("<changes><set path='/message'><value>CQF_Check_Message</value></set></changes>"), "", false)).message == "CQF_Check_Message", "loot messages accept CQF placeholders");
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Check_Dialog" };
        object dialog = changes.Build(tree, XElement.Parse("<changes><put path='/nodeMoulds'><key>1</key><value Class='QuestEditor_Library.DialogNode'><index>1</index><text>CQF_Check_Text</text></value></put><append path='/nodeMoulds/@0/options'><value Class='QuestEditor_Library.DialogOption'><text>CQF_Check_Option</text><results><li Class='QuestEditor_Library.DialogResult'><nextIndex>1</nextIndex></li></results></value></append></changes>"), "", false);
        Check(((DialogTreeDef)dialog).nodeMoulds.Count == 2 && tree.nodeMoulds.Count == 1, "dialogue draft adds linked nodes without mutating source");
        Check(model.Write(model.Copy(dialog), root: true).ToString() == model.Write(dialog, root: true).ToString(), "dialogue snapshot roundtrip");
        DialogTreeDef partial = (DialogTreeDef)changes.Build(tree, XElement.Parse("<changes><put path='/nodeMoulds'><key>2</key><value Class='QuestEditor_Library.DialogNode'><index>2</index><text>CQF_Check_Before</text></value></put><set path='/nodeMoulds/@2'><value><text>CQF_Check_After</text></value></set></changes>"), "", false);
        Check(partial.nodeMoulds[2].index == 2 && partial.nodeMoulds[2].text == "CQF_Check_After", "partial edit preserves fields of a newly inserted object");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><set path='/nodeMoulds/@0/text'><value>Invented narrative</value></set></changes>"), "", false), "new narrative rejected without permission");
        Check(((DialogTreeDef)changes.Build(tree, XElement.Parse("<changes><set path='/nodeMoulds/@0/text'><value>Supplied narrative</value></set></changes>"), "Supplied narrative", false)).nodeMoulds[0].text == "Supplied narrative", "supplied narrative allowed");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><set path='/nodeMoulds/@0/options'><value><li Class='System.Diagnostics.Process'/></value></set></changes>"), "", false), "unregistered executable type rejected");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><set path='/unknown'><value>1</value></set></changes>"), "", false), "unknown field rejected");
        DutyMapDef duty = new DutyMapDef { defName = "CQF_Check_Duty" };
        duty.CreateNode();
        string start = duty.startNodeId;
        DutyMapDef dutyDraft = (DutyMapDef)changes.Build(duty, XElement.Parse("<changes><append path='/nodes'><value Class='QuestEditor_Library.DutyMapNode'><nodeId>CQF_Check_Second</nodeId></value></append><append path='/transitions'><value Class='QuestEditor_Library.DutyMapTransition'><fromNodeId>" + start + "</fromNodeId><toNodeId>CQF_Check_Second</toNodeId><triggers><li Class='QuestEditor_Library.CustomDutyTrigger_Signal'><signal>CQF_Check_Signal</signal></li></triggers></value></append></changes>"), "", false);
        Check(dutyDraft.nodes.Count == 2 && duty.nodes.Count == 1, "duty graph nodes and transitions edit");
        Check(dutyDraft.nodes[0].overrideFacing == Rot4.Invalid, "invalid rotation sentinel is preserved");
        ComplexPawnDef pawn = new ComplexPawnDef { defName = "CQF_Check_Pawn" };
        XElement pawnChanges = XElement.Parse("<changes><append path='/modDatas'><value Class='QuestEditor_Library.PawnModData_Basic'/></append></changes>");
        ComplexPawnDef pawnDraft = (ComplexPawnDef)changes.Build(pawn, pawnChanges, "", false);
        Check(pawnDraft.modDatas.Count == pawn.modDatas.Count + 1, "NPC modules edit");
        QuestBookDef book = new QuestBookDef { defName = "CQF_Check_Book" };
        QuestBookDef bookDraft = (QuestBookDef)changes.Build(book, XElement.Parse("<changes><append path='/chapters'><value Class='QuestEditor_Library.QuestBookChapter'><id>CQF_Check_Chapter</id><steps><li Class='QuestEditor_Library.QuestBookStep'><id>CQF_Check_Step</id></li></steps></value></append></changes>"), "", false);
        Check(bookDraft.chapters.Count == book.chapters.Count + 1, "quest book chapters and steps edit");
        foreach (object target in new object[] { new DialogManagerDef { defName = "CQF_Check_Manager" }, new MainMapDef { defName = "CQF_Check_Main" }, new DutyDef { defName = "CQF_Check_DutyDef", thinkNode = new ThinkNode_Priority() }, new QuestScriptDef { defName = "CQF_Check_Quest", root = new QuestNode_Sequence() } })
        {
            object draft = changes.Build(target, XElement.Parse("<changes><set path='/label'><value>CQF_Check_Label</value></set></changes>"), "", false);
            Check(((Def)draft).label == "CQF_Check_Label", "edit " + target.GetType().Name);
            string snapshot = model.Write(target, root: true).ToString();
            Check(model.Write(model.Copy(target), root: true).ToString() == snapshot, "roundtrip " + target.GetType().Name);
        }
        TerrainDef floor = (TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef));
        floor.defName = "CQF_Check_Floor"; floor.label = "CQF_Check_FloorLabel"; floor.modContentPack = mod;
        ThingDef item = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        item.defName = "CQF_Check_Item"; item.label = "CQF_Check_ItemLabel"; item.stackLimit = 75; item.size = new IntVec2(1, 1); item.category = ThingCategory.Item; item.modContentPack = mod;
        DefDatabase<TerrainDef>.Add(floor);
        DefDatabase<ThingDef>.Add(item);
        QuestNode_DoCQFActions questNode = new QuestNode_DoCQFActions();
        questNode.inSignal = new SlateRef<string>("$inSignal");
        QuestNode_DoCQFActions nodeDraft = (QuestNode_DoCQFActions)changes.Build(questNode, XElement.Parse("<changes><set path='/inSignal'><value>$CQF_Check_Signal</value></set></changes>"), "", false);
        Check(((ISlateRef)nodeDraft.inSignal).SlateRef == "$CQF_Check_Signal" && ((ISlateRef)questNode.inSignal).SlateRef == "$inSignal", "QuestNode SlateRef fields are editable without evaluating expressions");
        CustomMapDataDef map = AICheckFixtures.Map();
        CustomMapDataDef mapDraft = (CustomMapDataDef)changes.Build(map, XElement.Parse("<changes><mapResize x='16' z='12'/><terrain def='CQF_Check_Floor' x='0' z='0' width='16' height='12'/><place def='CQF_Check_Item' x='4' z='5' count='2'/></changes>"), "", false);
        Check(mapDraft.terrainsRect[floor.defName].Sum(rect => rect.Area) == 192 && mapDraft.thingDatas.Single().position == new IntVec3(4, 0, 5) && map.thingDatas.Count == 0, "map generation uses loaded resources and preserves source");
        AIExportChecks.Run(model, (CustomMapDataDef)model.Copy(mapDraft));
        CustomMapDataDef erased = (CustomMapDataDef)changes.Build(mapDraft, XElement.Parse("<changes><erase x='4' z='5'/></changes>"), "", false);
        Check(erased.thingDatas.Count == 0 && mapDraft.thingDatas.Count == 1, "existing map editing");
        Reject(() => changes.Build(map, XElement.Parse("<changes><place def='MissingResource' x='0' z='0'/></changes>"), "", false), "missing map resource rejected");
        Reject(() => changes.Build(map, XElement.Parse("<changes><terrain def='CQF_Check_Floor' x='63' z='63' width='2'/></changes>"), "", false), "map coordinate overflow rejected");
        CQFAIResourceCatalog catalog = new CQFAIResourceCatalog(model);
        AIQueryChecks.Run(model, catalog, Path.Combine(game, "Mods", "CQF"));
        AIToolChecks.Run(model, catalog);
        AITokenChecks.Run(model, catalog);
        AILiveMapChecks.Run(model, catalog);
        AINetworkChecks.Run(model, catalog).GetAwaiter().GetResult();
        AIToolNetworkChecks.Run(model, catalog).GetAwaiter().GetResult();
        XElement resources = catalog.Query(XElement.Parse("<queries><defs type='Verse.ThingDef' search='CQF_Check'/></queries>"));
        Check(resources.Descendants("def").Any(def => def.Attribute("name")?.Value == item.defName && def.Attribute("mod")?.Value == "CQF.Checks"), "resource query returns actual Def and origin Mod");
        CQFAIConversation conversation = new CQFAIConversation(model, catalog);
        conversation.Add("user", "CQF_Check_Question");
        conversation.Add("assistant", "CQF_Check_Reply");
        conversation.Add("user", "CQF_Check_Followup");
        Check(conversation.Messages.Count == 3 && conversation.Instructions(null, false, false).Contains("Never return changes"), "continuous chat and read-only instructions");
        object current = tree;
        CQFAIEditorContext context = new CQFAIEditorContext("CQF_Check_Context", () => current, value => current = value);
        CQFAITransaction transaction = new CQFAITransaction(model, context);
        transaction.Build(XElement.Parse("<changes><set path='/label'><value>CQF_Check_NewLabel</value></set></changes>"), "", false);
        transaction.Build(XElement.Parse("<changes><set path='/title'><value>CQF_Check_RefinedTitle</value></set></changes>"), "", false);
        Check(((DialogTreeDef)transaction.Draft!).label == "CQF_Check_NewLabel" && ((DialogTreeDef)transaction.Draft).title == "CQF_Check_RefinedTitle" && ((DialogTreeDef)current).title != "CQF_Check_RefinedTitle", "follow-up command refines pending draft without losing previous changes");
        transaction.Apply();
        Check(((DialogTreeDef)current).label == "CQF_Check_NewLabel" && transaction.CanUndo, "apply draft with undo snapshot");
        transaction.Undo();
        Check(((DialogTreeDef)current).label == tree.label, "undo restores original data");
        CQFAITransaction stale = new CQFAITransaction(model, context);
        stale.Build(XElement.Parse("<changes><set path='/label'><value>CQF_Check_Label</value></set></changes>"), "", false);
        ((DialogTreeDef)current).title = "CQF_Check_Concurrent";
        Reject(stale.Apply, "concurrent modification rejects stale draft");
        object rollbackCurrent = model.Copy(tree);
        int attempts = 0;
        CQFAITransaction rollback = new CQFAITransaction(model, new CQFAIEditorContext("CQF_Check_Rollback", () => rollbackCurrent, value =>
        {
            rollbackCurrent = value;
            if (++attempts == 1) throw new InvalidOperationException("CQF_Check_ApplyFailure");
        }));
        rollback.Build(XElement.Parse("<changes><set path='/label'><value>CQF_Check_RollbackLabel</value></set></changes>"), "", false);
        Reject(rollback.Apply, "failed apply reports error");
        Check(((DialogTreeDef)rollbackCurrent).label == tree.label && !rollback.CanUndo && rollback.IsCurrent, "failed apply restores original target and allows retry");
        rollback.Apply(); Check(rollback.CanUndo, "draft can be retried after rollback");
        Reject(() => new CQFAIResponse("<assistant><queries/><changes/></assistant>"), "response cannot combine resource queries and changes");
        Reject(() => new CQFAIResponse("<assistant><reply>A</reply><reply>B</reply></assistant>"), "duplicate response sections rejected");
        Check(new CQFAIResponse("<assistant><reply>CQF_Check_Reply</reply></assistant>").Reply == "CQF_Check_Reply", "plain chat response parses independently of editing");
        conversation.Add("assistant", "<assistant><changes/></assistant>", "CQF_Check_VisibleReply");
        Check(conversation.Messages.Last().Content.Contains("changes") && conversation.Messages.Last().DisplayContent == "CQF_Check_VisibleReply", "conversation preserves protocol data with readable display");
        conversation.Add("system", "CQF_Check_TargetContext", "CQF_Check_TargetLabel");
        Check(conversation.Messages.Last().Content == "CQF_Check_TargetContext" && !conversation.VisibleMessages.Contains(conversation.Messages.Last()), "editor context stays in the protocol and is hidden from chat");
        conversation.Add("assistant", "<assistant><queries/></assistant>", "", false);
        Check(conversation.Messages.Last().Content.Contains("queries") && !conversation.VisibleMessages.Contains(conversation.Messages.Last()), "resource queries without a reply do not add chat clutter");
        conversation.Add("system", "CQF_Check_Error", "CQF_Check_VisibleError", true);
        Check(conversation.VisibleMessages.Last().DisplayContent == "CQF_Check_VisibleError", "errors remain visible in compact chat");
        conversation.Clear();
        Check(conversation.Messages.Count == 0 && !conversation.VisibleMessages.Any(), "clear conversation removes both visible and protocol history");
        Reject(() => CQFAIChanges.Parse("<!DOCTYPE root [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><assistant><reply>&x;</reply></assistant>"), "external XML entity blocked");
        foreach (Type type in new[] { typeof(DialogTreeDef), typeof(DutyMapDef), typeof(ComplexPawnDef), typeof(QuestBookDef), typeof(CustomMapDataDef), typeof(QuestScriptDef), typeof(DutyDef), typeof(MainMapDef) })
        {
            XElement schema = model.Schema(type);
            Check(schema.Elements().Any(), "schema for " + type.Name);
            Check(schema.ToString().Length < 500000, "schema fits request limit for " + type.Name);
            Console.WriteLine("Schema size " + type.Name + ": " + schema.ToString().Length);
        }
        Check(!CQFEditorBridge.IsLoaded && !CQFAIBridge.IsLoaded, "data checks do not initialize editor UI or AI entry");
        foreach (Type type in typeof(DialogTreeDef).Assembly.GetTypes().Where(type => !type.IsAbstract && (typeof(ISaveable).IsAssignableFrom(type) || typeof(IDrawable).IsAssignableFrom(type)) && !typeof(Thing).IsAssignableFrom(type)))
        {
            var supported = model.Fields(type).Select(field => field.Name).ToHashSet();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => !supported.Contains(field.Name) && !field.IsInitOnly && !field.IsDefined(typeof(UnsavedAttribute), true)
                && field.DeclaringType != typeof(Def) && field.Name is not ("origin" or "extraDataByDirection" or "extraDataByOrigin" or "idleNodes" or "parentIndex" or "subNodeIndexs" or "heights" or "scrollPos" or "buffer")))
                Console.WriteLine("Excluded field " + type.Name + "." + field.Name + ": " + field.FieldType);
        }
        Console.WriteLine("AI checks passed.");
    }
    private static void Check(bool result, string name)
    {
        if (!result) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException || error is System.Xml.XmlException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
