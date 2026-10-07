using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIRuntimeCatalogChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog resources)
    {
        QuestData data = new();
        data.SetBool("CQF_Check_Disabled", false); data.SetBool("CQF_Check_Enabled", true);
        data.SetValue("CQF_Check_Progress", 7);
        data.RecordTarget("CQF_Check_Cell", new TargetInfo(new IntVec3(4, 0, 6), null, true));
        List<Thing> members = new();
        for (int i = 0; i < 45; i++)
        {
            Thing thing = new() { thingIDNumber = 100 + i };
            ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = "CQF_Check_Member"; def.label = "CQF_Check_Label";
            thing.def = def; members.Add(thing);
        }
        data.AddGroup("CQF_Check_Group", members);
        XElement bools = CQFAIDatabaseCatalog.ReadData(data, "bools", "CQF_Check_Disabled", "", 0, 20);
        Check(bools.Elements("entry").Single().Attribute("value")?.Value == "false", "database inspection distinguishes a stored false value from a missing key");
        Check(CQFAIDatabaseCatalog.ReadData(data, "ints", "", "Progress", 0, 20).Elements("entry").Single().Attribute("value")?.Value == "7", "database values support exact keys and bounded substring discovery");
        Check(CQFAIDatabaseCatalog.ReadData(data, "targets", "", "", 0, 20).Descendants("target").Single().Attribute("x")?.Value == "4", "database inspection preserves cell targets without inventing Thing IDs");
        XElement groups = CQFAIDatabaseCatalog.ReadData(data, "groups", "", "", 0, 20);
        Check(groups.Elements("entry").Single().Attribute("count")?.Value == "45" && !groups.Descendants("target").Any(), "group discovery returns counts rather than dumping every member");
        XElement page = CQFAIDatabaseCatalog.ReadData(data, "group_members", "CQF_Check_Group", "", 40, 5);
        Check(page.Elements("member").Count() == 5 && page.Attribute("total")?.Value == "45" && page.Elements("member").First().Attribute("index")?.Value == "40", "database group membership is paginated with stable source indices");
        Check(page.Descendants("target").All(target => target.Attribute("def")?.Value == "CQF_Check_Member" && target.Attribute("spawned")?.Value == "false"), "database target inspection exposes current Thing identity and spawn state");
        Check(data.GetGroup("CQF_Check_Group").Count == 45 && data.GetValue("CQF_Check_Progress") == 7 && !data.GetBool("CQF_Check_Disabled"), "database queries preserve all original values and membership");
        QuestData empty = (QuestData)RuntimeHelpers.GetUninitializedObject(typeof(QuestData));
        Check(CQFAIDatabaseCatalog.ReadData(empty, "bools", "", "", 0, 20).Attribute("total")?.Value == "0"
            && typeof(QuestData).GetField("values_B", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(empty) == null,
            "database inspection does not initialize missing runtime collections");
        Reject(() => CQFAIDatabaseCatalog.ReadData(data, "group_members", "", "", 0, 20), "group membership inspection requires an exact existing group key");
        Reject(() => CQFAIDatabaseCatalog.ReadData(data, "group_members", "CQF_Check_Missing", "", 0, 20), "missing database groups return explicit errors");
        Reject(() => CQFAIDatabaseCatalog.ReadData(data, "unknown", "", "", 0, 20), "unknown database categories are rejected");
        Reject(() => CQFAIDatabaseCatalog.ReadData(data, "bools", "", "", -1, 20), "database inspection rejects invalid pagination");
        Reject(() => CQFAIDatabaseCatalog.List(0, 20), "database discovery reports absence of an active game instead of silently returning empty data");

        CustomMapDataDef definition = new() { defName = "CQF_Check_SignalMap" };
        CQFAction_SentSignal Signal(string value) => new() { signal = value, addQuestPrefix = true };
        definition.customThings.Add(new CustomThingData_CustomDoor { openingActions = new() { Signal("CQF_Check_Unlock") },
            comps = new() { new ActionComp { mode = ActionTriggerMode.Signal, signal = "CQF_Check_Unlock" } } });
        definition.customThings.Add(new CustomThingData_CustomContainer { openingActions = new() { Signal("CQF_Check_Container") } });
        definition.customThings.Add(new CustomThingData_CustomMapEntrance { enterActions = new() { Signal("CQF_Check_Entrance") } });
        definition.customThings.Add(new CustomThingData_CustomMapExit { enterActions = new() { Signal("CQF_Check_Exit") } });
        CQFSignalCatalog catalog = CQFSignalCatalog.Build(null, definition);
        XElement signals = CQFAIRuntimeCatalog.Signals(catalog, "Unlock", 0, 20);
        Check(signals.Elements("endpoint").Count() == 2 && signals.Elements("endpoint").All(endpoint => endpoint.Attribute("senders")?.Value == "1" && endpoint.Attribute("receivers")?.Value == "1"),
            "signal reference analysis joins matching sender and receiver scopes");
        Check(catalog.Entries.Any(entry => entry.Signal == "CQF_Check_Container") && catalog.Entries.Any(entry => entry.Signal == "CQF_Check_Entrance")
            && catalog.Entries.Any(entry => entry.Signal == "CQF_Check_Exit"), "signal catalog includes door, container and portal action lists");
        XElement signalPage = CQFAIRuntimeCatalog.Signals(catalog, "", 0, 1);
        Check(signalPage.Elements("endpoint").Count() == 1 && (bool)signalPage.Attribute("hasMore")! && signalPage.Attribute("scope")!.Value.Contains("not a signal execution trace"),
            "signal queries are paginated and state their verification scope");
        Reject(() => CQFAIRuntimeCatalog.Signals(catalog, new string('x', 101), 0, 20), "signal queries bound search input");
        CQFAIToolRegistry registry = new(model, resources, null, null, "", false, false);
        registry.RestrictToInspection();
        Check(registry.Tools.Any(tool => tool.Name == "cqf_list_databases") && registry.Tools.Any(tool => tool.Name == "cqf_read_database")
            && !registry.Tools.Any(tool => tool.Name == "cqf_apply_changes"), "inspection-only workers retain database discovery without write permission");
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        CQFAIEditorContext context = new("CQF_Check_MapReferences", () => new CQFAILiveMapInfo { backend = new CQFAILiveMap(map) }, _ => { });
        CQFAIToolRegistry mapRegistry = new(model, resources, context, null, "", false, false);
        mapRegistry.RestrictToInspection();
        Check(mapRegistry.Tools.Any(tool => tool.Name == "cqf_read_map_targets") && mapRegistry.Tools.Any(tool => tool.Name == "cqf_read_map_signals"),
            "actual map contexts expose target-key and signal queries to inspection-only workers");
        Reject(() => CQFAIRuntimeCatalog.Targets(map, "", "", 0, 41), "map target discovery validates bounds before reading map components");
    }
    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    { try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; } throw new InvalidOperationException("Expected rejection: " + name); }
}
