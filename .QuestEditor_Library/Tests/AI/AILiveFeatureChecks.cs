using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AILiveFeatureChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        PawnKindDef kind = (PawnKindDef)RuntimeHelpers.GetUninitializedObject(typeof(PawnKindDef)); kind.defName = "CQF_Check_Kind";
        DefDatabase<PawnKindDef>.Add(kind);
        LootBox box = NewThing<LootBox>();
        box.loots = new(); box.lootBoxName = "CQF_Check_Box"; box.openReport = "CQF_Check_Open"; box.tickToOpen = 100;
        CQFAILiveThingConfiguration before = CQFAILiveMap.Configuration(box);
        CQFAILiveThingConfiguration after = Change(before, "<set path='/loot/tickToOpen'><value>40</value></set><append path='/loot/loots'><value Class='QuestEditor_Library.LootData'><dataName>CQF_Check_Reward</dataName></value></append>");
        CQFAILiveMap.ApplyConfiguration(box, after);
        Check(box.tickToOpen == 40 && box.loots.Single().dataName == "CQF_Check_Reward" && !box.opened, "actual loot configuration changes do not open the box or execute rewards");
        FieldInfo cache = typeof(LootBox).GetField("innerLoot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        LootData chosen = box.loots.Single(); cache.SetValue(box, chosen);
        CQFAILiveThingConfiguration rename = Change(CQFAILiveMap.Configuration(box), "<set path='/loot/lootBoxName'><value>CQF_Check_Renamed</value></set>");
        CQFAILiveMap.ApplyConfiguration(box, rename);
        Check(ReferenceEquals(cache.GetValue(box), chosen), "unrelated box configuration edits preserve the selected reward cache");
        LootData? cached = CQFAILiveFeatures.CaptureLoot(box);
        CQFAILiveThingConfiguration rewardChange = Change(CQFAILiveMap.Configuration(box), "<set path='/loot/loots/0/dataName'><value>CQF_Check_ChangedReward</value></set>");
        CQFAILiveMap.ApplyConfiguration(box, rewardChange);
        Check(cache.GetValue(box) == null, "changing a reward table invalidates the runtime reward cache");
        CQFAILiveMap.ApplyConfiguration(box, rename); CQFAILiveFeatures.RestoreLoot(box, cached);
        Check(ReferenceEquals(cache.GetValue(box), chosen) && box.loots.Single().dataName == "CQF_Check_Reward", "reward restore restores both configuration and the previously selected reward");
        Check(CQFAIRuntimeCatalog.ThingState(box).Attribute("opened")?.Value == "false", "live reads expose opened state separately from editable reward configuration");
        Reject(() => CQFAILiveMap.ApplyConfiguration(box, Change(CQFAILiveMap.Configuration(box), "<set path='/loot/tickToOpen'><value>-1</value></set>")), "invalid loot timing is rejected before changing the live box");
        Check(box.tickToOpen == 40, "rejected loot configuration preserves current runtime values");

        CustomTrap_Capture trap = NewThing<CustomTrap_Capture>();
        trap.trapName = "CQF_Check_Trap"; trap.trapComps = new(); trap.disarmReport = "CQF_Check_Disarm"; trap.tickToDisarm = 100; trap.disarmActions = new();
        CQFAILiveThingConfiguration trapAfter = Change(CQFAILiveMap.Configuration(trap), "<set path='/trap/capture/tickToDisarm'><value>60</value></set><append path='/trap/trapComps'><value Class='QuestEditor_Library.TrapComp'><mode>Tick</mode><tick>120</tick></value></append>");
        CQFAILiveMap.ApplyConfiguration(trap, trapAfter);
        Check(trap.tickToDisarm == 60 && trap.trapComps.Single().tick == 120, "actual capture trap supports disarm and tick trigger configuration");
        Reject(() => Change(trapAfter, "<set path='/trap/trapComps/0/tick'><value>0</value></set>"), "trap tick configuration rejects nonpositive intervals");
        Reject(() => Change(trapAfter, "<set path='/trap/trapComps/0/mode'><value>Spawn</value></set>"), "trap configuration rejects trigger modes with no runtime notification path");
        CustomTrap plainTrap = NewThing<CustomTrap>(); plainTrap.trapName = "CQF_Check_Plain"; plainTrap.trapComps = new();
        Reject(() => CQFAILiveMap.ApplyConfiguration(plainTrap, trapAfter), "capture-only settings cannot be applied to a plain trap");

        CustomDoor door = NewThing<CustomDoor>(); door.openingActions = new(); door.openingConditions = new();
        CQFAILiveThingConfiguration doorAfter = Change(CQFAILiveMap.Configuration(door), "<append path='/door/openingConditions'><value Class='QuestEditor_Library.DialogCondition_Bool'><boolName>CQF_Check_Unlock</boolName></value></append>");
        CQFAILiveMap.ApplyConfiguration(door, doorAfter);
        Check(door.openingConditions.Count == 1, "actual door receives new opening conditions without opening or running actions");
        CustomContainer container = NewThing<CustomContainer>(); container.innerThings = new(); container.openingActions = new(); container.openingConditions = new(); container.tickToOpen = 100;
        CQFAILiveMap.ApplyConfiguration(container, Change(CQFAILiveMap.Configuration(container), "<set path='/container/tickToOpen'><value>20</value></set><append path='/container/innerThings'><value Class='QuestEditor_Library.LootData'><dataName>CQF_Check_Contents</dataName></value></append>"));
        Check(container.tickToOpen == 20 && container.innerThings.Count == 1, "container edits change generation recipes without creating or releasing held objects");

        QuestEditor_Library.Spawner spawner = NewThing<QuestEditor_Library.Spawner>();
        PawnSpawnData pawnData = (PawnSpawnData)RuntimeHelpers.GetUninitializedObject(typeof(PawnSpawnData));
        pawnData.kind = kind; pawnData.count = new IntRange(1, 1); pawnData.generationChance = 1; pawnData.actions = new();
        spawner.pawns = new() { pawnData };
        CQFAILiveMap.ApplyConfiguration(spawner, Change(CQFAILiveMap.Configuration(spawner), "<set path='/spawner/pawns/0/count'><value>2,3</value></set>"));
        Check(spawner.pawns.Single().count.min == 2 && spawner.pawns.Single().kind.defName == "CQF_Check_Kind", "actual spawner edits its NPC generation list without spawning NPCs");
        Reject(() => Change(CQFAILiveMap.Configuration(spawner), "<set path='/spawner/pawns/0/kind'><value null='true'/></set>"), "invalid NPC generation definitions are rejected before a runtime spawn can fail");

        ThingWithComps workerThing = NewThing<ThingWithComps>();
        CompActionWorker worker = new() { parent = workerThing };
        CompActionWorker second = new() { parent = workerThing };
        typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workerThing, new List<ThingComp> { new CompCustomText(), worker, second });
        CQFAILiveThingConfiguration workers = CQFAILiveMap.Configuration(workerThing);
        Check(workers.actionWorkers.Select(w => w.componentIndex).SequenceEqual(new[] { 1, 2 }), "live reads preserve actual component indices including multiple action workers");
        workers = Change(workers, "<append path='/actionWorkers/1/comps'><value Class='QuestEditor_Library.ActionComp'><mode>Signal</mode><signal>CQF_Check_ActualSignal</signal></value></append>");
        CQFAILiveMap.ApplyConfiguration(workerThing, workers);
        Check(second.comps.Single().signal == "CQF_Check_ActualSignal" && worker.comps.Count == 0, "editing one actual action worker preserves sibling components");
        Reject(() => CQFAILiveMap.ApplyConfiguration(workerThing, Change(workers, "<set path='/actionWorkers/1/componentIndex'><value>1</value></set>")), "action worker edits reject component reindexing");
        Reject(() => CQFAILiveMap.ApplyConfiguration(workerThing, Change(workers, "<set path='/actionWorkers/1/comps/0/mode'><value>Open</value></set>")), "opening triggers cannot be installed on ordinary objects with no Open event");

        AIFakeLiveMap fake = new(model); fake.Things["CQF_Check_FeatureBox"] = (CQFAILiveThingConfiguration)model.Copy(before);
        CQFAIHarness harness = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(fake), "", true, false);
        harness.Process(AIToolChecks.Response("feature_write", "cqf_edit_map_thing", ("thing_id", "CQF_Check_FeatureBox"), ("changes_xml", "<changes><set path='/loot/tickToOpen'><value>15</value></set></changes>")));
        harness.Process(AIToolChecks.Response("feature_read", "cqf_read_map_thing", ("thing_id", "CQF_Check_FeatureBox"), ("path", "/loot/tickToOpen")));
        Check(harness.LastResults.Single().Descendants("tickToOpen").Single().Value == "15", "live feature writes support narrow actual-state readback through the harness");
        harness.Transaction!.Undo(); Check(fake.Things["CQF_Check_FeatureBox"].loot!.tickToOpen == 100, "feature edits share the existing task undo transaction");
        fake.Things["CQF_Check_FeatureBox"].loot!.loots = Enumerable.Range(0, 200).Select(i => new LootData { dataName = "CQF_Check_" + i, message = new string('x', 300) }).ToList();
        harness.Process(AIToolChecks.Response("feature_summary", "cqf_read_map_thing", ("thing_id", "CQF_Check_FeatureBox")));
        Check(harness.LastResults.Single().Descendants("configurationSummary").Any(), "large live configurations default to bounded field summaries");
        harness.Process(AIToolChecks.Response("feature_page", "cqf_read_map_thing", ("thing_id", "CQF_Check_FeatureBox"), ("path", "/loot/loots"), ("offset", "198"), ("limit", "2")));
        Check(harness.LastResults.Single().Descendants("li").Count() == 2 && harness.LastResults.Single().Descendants("configurationData").Single().Attribute("total")?.Value == "200", "live configuration collection reads expose total and requested page only");
        Map inspectionMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)); inspectionMap.listerThings = new ListerThings(ListerThingsUse.Global); inspectionMap.components = new();
        inspectionMap.listerThings.AllThings.AddRange(new Thing[] { box, trap, workerThing });
        trap.trapComps.Single().tick = 0;
        XElement validation = new CQFAILiveMap(inspectionMap).Validate();
        Check(validation.Attribute("checkedThings")?.Value == "3" && validation.Attribute("errors")?.Value == "1"
            && validation.Elements("error").Single().Attribute("thingId")?.Value == trap.ThingID,
            "actual map validation reports invalid CQF runtime configuration with the affected Thing ID");
        CQFAIToolRegistry validationRegistry = new(model, catalog, null, null, "", false);
        validationRegistry.Register(new CQFAITool("cqf_validate_target", "CQF_Check_Validate", _ => validation));
        XElement failedValidation = validationRegistry.Execute(new CQFAIToolCall("CQF_Check_Validation", "cqf_validate_target", new XElement("arguments")));
        Check(failedValidation.Attribute("success")?.Value == "false" && failedValidation.Element("error")!.Value.Contains(trap.ThingID),
            "failed configuration validation exposes failure and the affected object in tool progress");
        CQFAITaskState task = new(); task.RecordTool("cqf_edit_map_thing", true);
        task.RecordTool("cqf_validate_target", (bool?)failedValidation.Attribute("success") == true);
        Check(task.RequiresVerification, "failed configuration validation cannot satisfy task verification");
        trap.trapComps.Single().tick = 120;
        Check(new CQFAILiveMap(inspectionMap).Validate().Attribute("errors")?.Value == "0", "actual configuration validation reflects corrections without replaying triggers");
        CQFAILiveThingConfiguration Change(CQFAILiveThingConfiguration source, string operations) => (CQFAILiveThingConfiguration)new CQFAIChanges(model).Build(source, XElement.Parse("<changes>" + operations + "</changes>"), "", true);
    }
    private static T NewThing<T>() where T : Thing
    {
        T thing = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "CQF_Check_Feature_" + typeof(T).Name; def.thingClass = typeof(T); def.stackLimit = 1; def.size = new IntVec2(1, 1);
        thing.def = def; thing.stackCount = 1;
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(thing, (sbyte)-1);
        return thing;
    }
    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    { try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; } throw new InvalidOperationException("Expected rejection: " + name); }
}
