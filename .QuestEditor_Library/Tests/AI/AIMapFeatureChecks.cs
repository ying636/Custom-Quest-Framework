using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIMapFeatureChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { Size = new IntVec3(32, 1, 32) }; map.components = new();
        map.mapPawns = new MapPawns(map); map.listerThings = new ListerThings(ListerThingsUse.Global);
        MapComponent_CustomMapData component = new(map); map.components.Add(component);
        Game? oldGame = Current.Game;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new List<Map> { map });
        Current.Game = game;
        try
        {
            Building building = Thing<Building>("CQF_Check_MapBuilding");
            ThingWithComps ordinary = Thing<ThingWithComps>("CQF_Check_MapOrdinary");
            CQFEventArea original = new() { key = "CQF_Check_Area", cells = new() { new IntVec3(2, 0, 2) }, actions = new() };
            original.InitializeRuntime(map); component.EventAreas.Add(original);
            ThingActionTrigger originalTrigger = new() { key = "CQF_Check_Trigger", mode = ActionTriggerMode.Damaged, things = new() { building }, actions = new() };
            component.Triggers.Add(originalTrigger);
            Check(CQFAIMapFeatures.Read(map).triggers.Single().thingIds.Single() == building.ThingID, "map trigger reads expose exact actual Thing IDs");
            string editArea = "<set path='/eventAreas/0/cells/0'><value>3,0,4</value></set>";
            CQFAILiveMapEdit edit = Prepare(editArea); edit.Apply();
            CQFEventArea changed = component.EventAreas.Single();
            HashSet<IntVec3> cache = (HashSet<IntVec3>)typeof(CQFEventArea).GetField("cellSet", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(changed)!;
            Check(cache.SetEquals(new[] { new IntVec3(3, 0, 4) }) && edit.IsCurrent, "event-area edits update runtime cell caches and actual state without triggering actions");
            HashSet<IntVec3> originalCache = (HashSet<IntVec3>)typeof(CQFEventArea).GetField("cellSet", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(original)!;
            originalCache.Add(new IntVec3(20, 0, 20));
            edit.Undo(); Check(ReferenceEquals(component.EventAreas.Single(), original) && ReferenceEquals(component.Triggers.Single(), originalTrigger), "map configuration undo restores original area and trigger instances");
            Check(((HashSet<IntVec3>)typeof(CQFEventArea).GetField("cellSet", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(original)!).SetEquals(original.cells), "undo refreshes restored event-area runtime caches instead of reusing stale occupancy");
            CQFAILiveMapEdit add = Prepare("<append path='/eventAreas'><value Class='QuestEditor_Library.CQFEventArea'><key>CQF_Check_NewArea</key><cells><li>5,0,5</li></cells></value></append>");
            add.Apply(); Check(component.EventAreas.Count == 2 && ReferenceEquals(component.EventAreas[0], original), "adding an event area preserves unchanged runtime areas and their caches");
            add.Undo(); Check(component.EventAreas.Count == 1, "new event areas are removed by task undo");
            CQFAILiveMapEdit remove = Prepare("<remove path='/eventAreas/0'/><remove path='/triggers/0'/>");
            remove.Apply(); Check(component.EventAreas.Count == 0 && component.Triggers.Count == 0, "removing configuration entries deletes actual areas and triggers");
            remove.Undo(); Check(component.EventAreas.Count == 1 && component.Triggers.Count == 1, "deleted map configuration is restored by undo");
            Reject(() => Prepare("<set path='/eventAreas/0/cells/0'><value>32,0,1</value></set>"), "out-of-bounds event areas are rejected before any runtime mutation");
            Reject(() => Prepare("<set path='/eventAreas/0/faction'><value>CQF_Check_MissingFaction</value></set>"), "event areas reject missing faction definitions");
            Reject(() => Prepare("<set path='/triggers/0/mode'><value>Tick</value></set>"), "map triggers reject modes with no runtime notification path");
            Reject(() => Prepare("<set path='/triggers/0/thingIds/0'><value>" + ordinary.ThingID + "</value></set>"), "map damage triggers reject objects without a Building damage notification path");
            Reject(() => Prepare("<set path='/triggers/0/thingIds/0'><value>CQF_Check_MissingThing</value></set>"), "map triggers reject missing actual object IDs");
            CQFAIMapConfiguration duplicate = (CQFAIMapConfiguration)model.Copy(CQFAIMapFeatures.Read(map)); duplicate.eventAreas.Add(original);
            Reject(() => CQFAIMapFeatures.Validate(duplicate), "duplicate event area keys are rejected");
            CQFAILiveMapEdit stale = Prepare(editArea); original.cells[0] = new IntVec3(7, 0, 7);
            Reject(() => stale.Apply(), "map config rejects state changed after preparation"); stale.Undo();
            Check(original.cells[0] == new IntVec3(7, 0, 7), "failed stale apply and rollback preserve intervening edits");
            original.cells[0] = new IntVec3(2, 0, 2); original.InitializeRuntime(map);

            CompCustomText first = new() { parent = ordinary }, second = new() { parent = ordinary };
            ordinary.def.comps = new() { new CompProperties(typeof(CompCustomText)) };
            XElement discovery = CQFAIThingCatalog.Describe(ordinary.def)!;
            Check(discovery.Attribute("kind")?.Value == "custom_text" && (bool)discovery.Attribute("liveFeatureEditing")!
                && discovery.Element("liveComponentConfiguration")?.Attribute("path")?.Value == "/customTexts", "custom text discovery exposes the actual editable component path");
            typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ordinary, new List<ThingComp> { first, new CompActionWorker { parent = ordinary }, second });
            CQFAILiveThingConfiguration before = (CQFAILiveThingConfiguration)model.Copy(CQFAILiveMap.Configuration(ordinary));
            CQFAILiveThingConfiguration after = Change(before, "<set path='/customTexts/1/customName'><value>CQF_Check_NewName</value></set><set path='/customTexts/1/useCustomName'><value>true</value></set><append path='/extraInteractions'><value Class='QuestEditor_Library.InteractionOperation'><interactionText>CQF_Check_Use</interactionText></value></append>");
            CQFAILiveMap.ApplyConfiguration(ordinary, after);
            Check(second.useCustomName && second.customName == "CQF_Check_NewName" && !first.useCustomName && component.ExtraOperations[ordinary].Count == 1,
                "ordinary objects gain actual attached interactions and independently indexed custom text");
            Check(CQFAILiveMap.Configuration(ordinary).extraInteractions.Single().interactionText == "CQF_Check_Use", "actual attached interactions are available to readback");
            Reject(() => CQFAILiveMap.ApplyConfiguration(ordinary, Change(after, "<set path='/customTexts/1/componentIndex'><value>0</value></set>")), "custom text component reindexing is rejected before modifying siblings");
            Reject(() => Change(after, "<set path='/customTexts/1/customName'><value></value></set>"), "enabled custom labels cannot be empty");
            CQFAILiveMap.ApplyConfiguration(ordinary, before);
            Check(!component.ExtraOperations.ContainsKey(ordinary) && !second.useCustomName && second.customName == null, "restoring actual configuration removes newly attached interactions and restores text state");

            ZoneCore zone = Thing<ZoneCore>("CQF_Check_MapZone"); zone.size = CoreSize.Empty; zone.conditions = new(); zone.coreTags = new(); zone.isCenter = true; zone.coreRotation = Rot4.Invalid;
            CQFAILiveThingConfiguration zoneBefore = CQFAILiveMap.Configuration(zone);
            CQFAILiveThingConfiguration zoneAfter = Change(zoneBefore, "<set path='/zone/isCenter'><value>false</value></set><set path='/zone/coreRotation'><value>1</value></set><append path='/zone/coreTags'><value>CQF_Check_Tag</value></append><set path='/zone/size/maxX'><value>4</value></set>");
            CQFAILiveMap.ApplyConfiguration(zone, zoneAfter);
            Check(!zone.isCenter && zone.coreRotation == Rot4.East && zone.coreTags.Single() == "CQF_Check_Tag" && zone.size.maxX == 4, "actual ZoneCore edits preserve explicit docking dimensions and orientation");
            Reject(() => Change(zoneAfter, "<set path='/zone/size/minZ'><value>-1</value></set>"), "ZoneCore dimensions reject negative extents");
            Reject(() => Change(zoneAfter, "<set path='/zone/coreRotation'><value>" + Rot4.Invalid.AsInt + "</value></set>"), "non-center docking cores require a valid direction");
            GenerationActionWorker generation = Thing<GenerationActionWorker>("CQF_Check_MapGeneration"); generation.actions = new();
            CQFAILiveMap.ApplyConfiguration(generation, Change(CQFAILiveMap.Configuration(generation), "<append path='/generation/actions'><value Class='QuestEditor_Library.CQFAction_SentSignal'><signal>CQF_Check_Ready</signal></value></append>"));
            Check(generation.actions.Single() is CQFAction_SentSignal signal && signal.signal == "CQF_Check_Ready", "actual GenerationActionWorker actions are editable without execution");

            CQFAILiveMap backend = new(map);
            CQFAIEditorContext context = new("CQF_Check_MapFeatures", () => new CQFAILiveMapInfo { backend = backend, mapId = map.uniqueID, size = map.Size },
                _ => throw new InvalidDataException("CQF_AI_LiveMapOperationRequired"), isValid: () => backend.IsValid, identity: map);
            CQFAILiveMapTransaction transaction = new(model, context, backend);
            CQFAIToolRegistry registry = new(model, catalog, context, transaction, "", true);
            XElement query = registry.Execute(new CQFAIToolCall("CQF_Check_MapRead", "cqf_read_map_configuration", new XElement("arguments", new XElement("path", "/eventAreas/0/cells"))));
            Check(query.Descendants("li").Single().Value == "2,0,2", "map configuration tool returns requested actual field pages");
            XElement receipt = registry.Execute(new CQFAIToolCall("CQF_Check_MapEdit", "cqf_edit_map_configuration", new XElement("arguments", new XElement("changes_xml", "<changes>" + editArea + "</changes>"))));
            Check((bool)receipt.Attribute("success")! && component.EventAreas.Single().cells[0] == new IntVec3(3, 0, 4), "map configuration writes pass through the actual tool transaction and return readback receipts");
            Check(new CQFAILiveMap(map).Validate().Attribute("errors")?.Value == "0", "live validation includes new map and Thing configurations");
            transaction.Undo(); Check(ReferenceEquals(component.EventAreas.Single(), original), "actual tool writes restore original runtime state through transaction undo");
            registry.RestrictToInspection();
            Check(registry.Tools.Any(tool => tool.Name == "cqf_read_map_configuration") && !registry.Tools.Any(tool => tool.Name == "cqf_edit_map_configuration"), "inspection-only agents receive new map queries without write permissions");
            Reject(() => registry.Execute(new CQFAIToolCall("CQF_Check_MapReadBounds", "cqf_read_map_configuration", new XElement("arguments", new XElement("limit", "41")))), "map configuration summary reads enforce pagination bounds");
            Reject(() => new CQFAILiveMapPlan(backend, model, "", true).Build(XElement.Parse("<changes><editMap><changes>" + editArea + "</changes></editMap><erase x='1' z='1'/></changes>")), "map configuration batches cannot interleave object removal with trigger bindings");
            CQFAITaskState task = new(); task.RecordTool("cqf_edit_map_configuration", true); Check(task.RequiresVerification, "map configuration writes require task verification");
            task.RecordTool("cqf_read_map_configuration", true); Check(!task.RequiresVerification, "actual map configuration readback satisfies task verification");
            CQFAIMapConfiguration many = new() { eventAreas = Enumerable.Range(0, 45).Select(i => new CQFEventArea { key = "CQF_Check_Page_" + i, cells = new() { new IntVec3(i % 32, 0, 1) } }).ToList() };
            XElement page = new CQFAITargetReader(model).Read(many, "/eventAreas", 40, 5);
            Check(page.Attribute("total")?.Value == "45" && page.Descendants("li").Count(element => element.Parent?.Name == "eventAreas") == 5, "map event configuration supports bounded collection pagination");

            CQFAILiveMapEdit Prepare(string operations) => new CQFAILiveMapPlan(new CQFAILiveMap(map), model, "", true).Build(XElement.Parse("<changes><editMap><changes>" + operations + "</changes></editMap></changes>")).Single();
            CQFAILiveThingConfiguration Change(CQFAILiveThingConfiguration source, string operations) => (CQFAILiveThingConfiguration)new CQFAIChanges(model).Build(source, XElement.Parse("<changes>" + operations + "</changes>"), "", true);
            T Thing<T>(string name) where T : Verse.Thing
            {
                T thing = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
                ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = name; def.label = name; def.size = new IntVec2(1, 1); def.stackLimit = 1;
                def.thingClass = typeof(T); def.category = ThingCategory.Building; thing.def = def; thing.stackCount = 1;
                typeof(Verse.Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(thing, (sbyte)0);
                map.listerThings.AllThings.Add(thing); return thing;
            }
        }
        finally { Current.Game = oldGame!; }
    }
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    { try { action(); } catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException) { Console.WriteLine("PASS " + name); return; } throw new InvalidOperationException("Expected rejection: " + name); }
}
