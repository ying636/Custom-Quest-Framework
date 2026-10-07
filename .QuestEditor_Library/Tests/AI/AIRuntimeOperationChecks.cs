using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class AIRuntimeOperationChecks
{
    public static void Run(CQFAIModel originalModel, CQFAIResourceCatalog resources)
    {
        ModContentPack mod = LoadedModManager.RunningModsListForReading.First();
        mod.assemblies.loadedAssemblies.Add(typeof(CQFCheckRuntimeAction).Assembly); GenTypes.ClearCache();
        CQFAIModel model = new();
        FieldInfo bindingField = typeof(DefOfHelper).GetField("bindingNow", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? oldBinding = bindingField.GetValue(null);
        try { bindingField.SetValue(null, true); RuntimeHelpers.RunClassConstructor(typeof(ThingDefOf).TypeHandle); RuntimeHelpers.RunClassConstructor(typeof(MessageTypeDefOf).TypeHandle); RuntimeHelpers.RunClassConstructor(typeof(DutyDefOf).TypeHandle); }
        finally { bindingField.SetValue(null, oldBinding); }
        Game? oldGame = Current.Game; GameComponent_Editor? oldEditor = GameComponent_Editor.Instance; GameComponent_ComplexDuty? oldDuty = GameComponent_ComplexDuty.Instance; GameComponent_QuestBook? oldBook = GameComponent_QuestBook.Instance;
        FieldInfo rootField = typeof(CQFContentPaths).GetField("root", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? oldRoot = rootField.GetValue(null);
        string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CQF_Runtime_Checks_" + Guid.NewGuid().ToString("N")));
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)); map.info = new MapInfo { Size = new IntVec3(30, 1, 30) }; map.uniqueID = 791;
        map.components = new(); map.listerThings = new ListerThings(ListerThingsUse.Global); map.mapPawns = new MapPawns(map);
        DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "CQF_Check_RuntimeDesignation" });
        map.cellIndices = new CellIndices(map); map.designationManager = new DesignationManager(map);
        MapComponent_CustomMapData mapData = new(map); map.components.Add(mapData); map.components.Add(new MapComponent_CQFTargets(map)); map.components.Add(new MapComponent_CQFComponentOverrides(map));
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map> { map });
        game.questManager = new QuestManager(); game.signalManager = new SignalManager(); game.components = new();
        game.uniqueIDsManager = new UniqueIDsManager();
        Current.Game = game;
        GameComponent_Editor editor = new(game); GameComponent_ComplexDuty duties = new(game); GameComponent_QuestBook books = new(game); game.components.AddRange(new GameComponent[] { editor, duties, books });
        Directory.CreateDirectory(directory); CQFContentPaths.Initialize(directory);
        try
        {
            CQFAIToolRegistry readOnly = Registry(false);
            Check(readOnly.Tools.Any(tool => tool.Name == "cqf_read_runtime") && readOnly.Tools.All(tool => tool.Name is not ("cqf_operate_runtime" or "cqf_manage_definition")), "runtime read tools exist without an editing target and write tools respect disabled editing");
            CQFAIToolRegistry registry = Registry(true);
            Check(registry.Tools.Any(tool => tool.Name == "cqf_operate_runtime"), "runtime operations do not require an editor target");
            XElement capabilities = Call("cqf_get_context").Descendants("capabilities").Single();
            Check(capabilities.Attribute("hasTarget")?.Value == "false" && capabilities.Attribute("runtimeOperations")?.Value == "true", "context permissions explicitly distinguish missing editor selection from available runtime editing");
            foreach (string group in new[] { "pawns", "duties", "quests", "world", "database", "inventory", "facilities", "execution", "definitions", "diagnostics" })
                Check(Call("cqf_runtime_help", new XElement("group", group)).Elements().Any(), "runtime discovery supplies schemas for " + group);
            CQFAIToolRegistry worker = Registry(true); worker.RestrictToInspection();
            Check(worker.Tools.Any(tool => tool.Name == "cqf_check_conditions") && worker.Tools.All(tool => tool.Name is not ("cqf_operate_runtime" or "cqf_manage_definition")), "inspection workers keep runtime diagnostics and cannot execute operations or save files");
            Reject(() => Read("<kind>maps</kind><map_id>-2</map_id>"), "negative explicit map IDs are rejected");
            Reject(() => CQFAIRuntimeRequest.Parse("<operation><kind>a</kind><kind>b</kind></operation>", "operation"), "duplicate runtime fields are rejected");
            Reject(() => CQFAIRuntimeRequest.Targets(XElement.Parse("<operation><targets><target><key>K</key><thing_id>T</thing_id><map_id>791</map_id></target></targets></operation>")), "Thing/cell target ambiguity is rejected before execution");

            Pawn pawn = Thing<Pawn>("CQF_Check_RuntimePawn"); pawn.health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
            PawnKindDef kind = new() { defName = "CQF_Check_RuntimeKind" }; pawn.kindDef = kind;
            Check(Read("<kind>duty</kind><pawn_id>" + pawn.ThingID + "</pawn_id>").Descendants("duty").Single().Attribute("initialized")?.Value == "false", "duty query reports absent runtime without creating it");
            Check(CQFAIPawnRuntime.Runtime(pawn) == null, "read-only duty inspection preserves missing runtime");
            SkillDef skillDef = new() { defName = "CQF_Check_RuntimeSkill" }; DefDatabase<SkillDef>.Add(skillDef);
            pawn.skills = (Pawn_SkillTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_SkillTracker)); SkillRecord skill = new(pawn, skillDef) { levelInt = 3, passion = Passion.Minor, xpSinceLastLevel = 500 }; pawn.skills.skills = new() { skill };
            Operate("<kind>pawn_skill</kind><pawn_id>" + pawn.ThingID + "</pawn_id><skill>" + skillDef.defName + "</skill><level>12</level><passion>Major</passion>");
            Check(skill.levelInt == 12 && skill.passion == Passion.Major && skill.xpSinceLastLevel == 0, "skill operation changes the actual existing Pawn rather than a template");
            Reject(() => Operate("<kind>pawn_skill</kind><pawn_id>" + pawn.ThingID + "</pawn_id><skill>" + skillDef.defName + "</skill><level>21</level>"), "skill bounds reject before mutation");
            registry.RuntimeJournal.Undo(); Check(skill.levelInt == 3 && skill.passion == Passion.Minor && skill.xpSinceLastLevel == 500, "Pawn skill undo restores level passion and XP");
            foreach (string queryKind in new[] { "pawn_profile", "pawn_skills", "pawn_health", "pawn_traits", "pawn_genes", "pawn_abilities", "pawn_needs", "pawn_dialogue" })
                Check(Read("<kind>" + queryKind + "</kind><pawn_id>" + pawn.ThingID + "</pawn_id>").Attribute("success")?.Value == "true", "actual Pawn state inspection supports " + queryKind);
            Check(pawn.story == null && pawn.genes == null && pawn.abilities == null && pawn.needs == null, "optional Pawn inspections do not initialize missing trackers");
            DialogManagerDef manager = new() { defName = "CQF_Check_RuntimeManager" }; DefDatabase<DialogManagerDef>.Add(manager);
            registry = Registry(true); Operate("<kind>pawn_dialogue</kind><pawn_id>" + pawn.ThingID + "</pawn_id><definition>" + manager.defName + "</definition>");
            Check(editor.dialogsWithTargets[pawn] == manager && Read("<kind>pawn_dialogue</kind><pawn_id>" + pawn.ThingID + "</pawn_id>").Descendants("dialogue").Single().Attribute("definition")?.Value == manager.defName, "Pawn dialogue binding changes the actual component and reads back without opening a window");
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn, (sbyte)-1);
            Check(Read("<kind>pawns</kind><scope>all</scope>").Descendants("pawn").Any(element => element.Attribute("id")?.Value == pawn.ThingID) && CQFAIRuntimeRequest.Pawn(new XElement("request", new XElement("pawn_id", pawn.ThingID))) == pawn, "global NPC discovery can resolve an unspawned dialogue-bound Pawn");
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn, (sbyte)0);
            registry.RuntimeJournal.Undo(); Check(!editor.dialogsWithTargets.ContainsKey(pawn), "dialogue binding undo restores prior absence");

            registry = Registry(true);
            FieldInfo global = typeof(GameComponent_Editor).GetField("globalData", BindingFlags.NonPublic | BindingFlags.Instance)!;
            global.SetValue(editor, null);
            Call("cqf_list_databases"); Check(global.GetValue(editor) == null, "listing databases does not initialize Global data");
            Reject(() => Operate("<kind>database_value</kind><key>CQF_Check_Flag</key><value_type>bool</value_type><value>true</value>"), "uninitialized database writes require explicit creation");
            Operate("<kind>database_create</kind>");
            Operate("<kind>database_value</kind><key>CQF_Check_Flag</key><value_type>bool</value_type><value>true</value>");
            Operate("<kind>database_value</kind><key>CQF_Check_Count</key><value_type>int</value_type><value>7</value>");
            Operate("<kind>database_group</kind><key>CQF_Check_Group</key><members><id>" + pawn.ThingID + "</id></members>");
            QuestData data = (QuestData)global.GetValue(editor)!;
            Check(data.GetBool("CQF_Check_Flag") && data.GetValue("CQF_Check_Count") == 7 && data.GetGroup("CQF_Check_Group").Single() == pawn, "database operations change real flags integers and group membership");
            registry.RuntimeJournal.Undo(); Check(global.GetValue(editor) == null, "mixed database task undo restores the original missing slot");
            registry = Registry(true); global.SetValue(editor, new QuestData());
            Operate("<kind>database_value</kind><key>CQF_Check_Concurrent</key><value_type>int</value_type><value>1</value>");
            ((QuestData)global.GetValue(editor)!).SetValue("CQF_Check_Concurrent", 8);
            Check(!registry.RuntimeJournal.IsCurrent, "manual database changes invalidate AI undo fingerprints");
            Reject(registry.RuntimeJournal.Undo, "conflicting task undo refuses to erase later database edits");
            global.SetValue(editor, null); registry = Registry(true); Operate("<kind>database_create</kind>");
            ((QuestData)global.GetValue(editor)!).SetValue("CQF_Check_ManualAfterCreation", 9);
            Check(!registry.RuntimeJournal.IsCurrent, "new database undo detects later edits inside the created database");
            Reject(registry.RuntimeJournal.Undo, "database creation undo cannot erase later manual entries");
            CustomDutyMap dutyRuntime = duties.GetRuntime(pawn);
            registry = Registry(true); Operate("<kind>duty_value</kind><pawn_id>" + pawn.ThingID + "</pawn_id><key>CQF_Check_DutyFloat</key><value_type>float</value_type><value>1.5</value>");
            Check(dutyRuntime.GetFloat("CQF_Check_DutyFloat") == 1.5f, "duty database edits reach actual Pawn-private values"); registry.RuntimeJournal.Undo();
            Check(!dutyRuntime.HasKey("CQF_Check_DutyFloat"), "duty variable undo restores absence rather than a fabricated zero entry");

            Building ordinary = Thing<Building>("CQF_Check_RuntimeBuilding"); registry = Registry(true);
            Operate("<kind>map_target</kind><map_id>791</map_id><key>CQF_Check_BuildingKey</key><thing_id>" + ordinary.ThingID + "</thing_id>");
            Check(map.GetComponent<MapComponent_CQFTargets>().GetTarget("CQF_Check_BuildingKey").Thing == ordinary, "map target operation records an actual map object");
            Reject(() => Operate("<kind>map_target</kind><map_id>791</map_id><key>CustomThing</key><thing_id>" + ordinary.ThingID + "</thing_id>"), "reserved map target names remain protected"); registry.RuntimeJournal.Undo();
            Check(!map.GetComponent<MapComponent_CQFTargets>().GetTarget("CQF_Check_BuildingKey").IsValid, "map target undo removes the new key");

            ThingWithComps facility = Thing<ThingWithComps>("CQF_Check_RuntimeFacility"); CompPropertiesTriggerDialog shared = new() { triggerSignal = "CQF_Check_Before" };
            CompTriggerDialog trigger = new() { parent = facility, props = shared }; CompHackOutcome hack = new() { parent = facility, props = new CompPropertiesHackOutcome() };
            typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(facility, new List<ThingComp> { trigger, hack });
            DialogTreeDef dialog = new() { defName = "CQF_Check_RuntimeTriggerDialog", label = "CQF_Check_RuntimeTriggerDialog", modContentPack = mod }; DefDatabase<DialogTreeDef>.Add(dialog);
            registry = Registry(true);
            Operate("<kind>facility_configure</kind><thing_id>" + facility.ThingID + "</thing_id><component_index>0</component_index><changes_xml>" + new XCData("<changes><set path='/triggerSignal'><value>CQF_Check_After</value></set><set path='/dialog'><value def='QuestEditor_Library.DialogTreeDef'>" + dialog.defName + "</value></set></changes>").ToString() + "</changes_xml>");
            Check(trigger.Props.triggerSignal == "CQF_Check_After" && trigger.Props.dialog == dialog && shared.triggerSignal == "CQF_Check_Before", "facility configuration clones per-instance props and leaves shared definition props untouched");
            CQFComponentOverride persisted = map.GetComponent<MapComponent_CQFComponentOverrides>().Get(facility, 0)!;
            Check(persisted.triggerSignal == "CQF_Check_After" && persisted.dialog == dialog, "live facility Props edits record framework-owned save data");
            trigger.props = shared; Check(persisted.Apply() && trigger.Props.triggerSignal == "CQF_Check_After" && trigger.Props.dialog == dialog, "framework restores overridden component Props without editor participation");
            Check(!new CQFComponentOverride { thing = facility, componentIndex = 1, componentType = trigger.GetType().FullName! }.Apply(), "saved facility configuration refuses a mismatched physical component");
            string componentSave = Path.Combine(directory, "CQF_Check_ComponentSave.xml");
            MapComponent_CQFComponentOverrides? savedComponent = map.GetComponent<MapComponent_CQFComponentOverrides>();
            try
            {
                Scribe.saver.InitSaving(componentSave, "savegame"); Scribe_Deep.Look(ref savedComponent, "component", map); Scribe.saver.FinalizeSaving();
                XDocument saved = XDocument.Load(componentSave);
                Check(saved.Descendants("triggerSignal").Single().Value == "CQF_Check_After" && saved.Descendants("thing").Single().Value == facility.GetUniqueLoadID(), "native Scribe writes component settings and real Thing references");
                MapComponent_CQFComponentOverrides? loadedComponent = null;
                Scribe.loader.InitLoading(componentSave); Scribe_Deep.Look(ref loadedComponent, "component", map);
                LoadedObjectDirectory objects = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
                objects.RegisterLoaded(facility); Scribe.loader.FinalizeLoading(); trigger.props = shared;
                loadedComponent!.FinalizeInit();
                Check(loadedComponent.Get(facility, 0)?.dialog == dialog && trigger.Props.triggerSignal == "CQF_Check_After", "native Scribe round-trip resolves Def/Thing references and framework restores live settings");
            }
            finally { if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop(); }
            Reject(() => Operate("<kind>facility_configure</kind><thing_id>" + facility.ThingID + "</thing_id><component_index>0</component_index><changes_xml><![CDATA[<changes><set path='/receiverId'><value>CQF_Check_X</value></set></changes>]]></changes_xml>"), "facility edits reject fields belonging to another component");
            registry.RuntimeJournal.Undo(); Check(ReferenceEquals(trigger.props, shared) && map.GetComponent<MapComponent_CQFComponentOverrides>().Get(facility, 0) == null, "facility undo restores the exact original props instance and removes new saved overrides");
            CompPropertiesSetMapAndGenerate choiceProps = new() { key = "CQF_Check_OriginalExit", map = new CustomMapGenerationSet() };
            CompSetMapAndGenerate choice = new() { parent = facility, props = choiceProps }; facility.AllComps.Add(choice);
            registry = Registry(true); Operate("<kind>facility_configure</kind><thing_id>" + facility.ThingID + "</thing_id><component_index>2</component_index><changes_xml><![CDATA[<changes><set path='/exitKey'><value>CQF_Check_NewExit</value></set></changes>]]></changes_xml>");
            Check(choice.Props.key == "CQF_Check_NewExit" && choiceProps.key == "CQF_Check_OriginalExit", "map selection component is editable per instance without replaying its creation callback");
            registry.RuntimeJournal.Undo(); Check(choice.props == choiceProps, "map selection configuration participates in actual task undo");
            ThingWithComps powerInput = Thing<ThingWithComps>("CQF_Check_PowerInput"), powerOutput = Thing<ThingWithComps>("CQF_Check_PowerOutput");
            CompPower_Level inputPower = new() { parent = powerInput, props = new CompProperties_Power(), outputMode = false }, outputPower = new() { parent = powerOutput, props = new CompProperties_Power(), outputMode = true };
            typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(powerInput, new List<ThingComp> { inputPower });
            typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(powerOutput, new List<ThingComp> { outputPower });
            registry = Registry(true); Operate("<kind>facility_configure</kind><thing_id>" + powerOutput.ThingID + "</thing_id><component_index>0</component_index><changes_xml>" + new XCData("<changes><set path='/linkedPowerId'><value>" + powerInput.ThingID + "</value></set><set path='/targetPowerOutput'><value>1500</value></set></changes>").ToString() + "</changes_xml>");
            Check(outputPower.linked == powerInput && inputPower.linked == powerOutput && outputPower.comp == inputPower && inputPower.comp == outputPower && outputPower.targetPowerOutput == 1500, "power facility linking updates both actual components and their caches");
            registry.RuntimeJournal.Undo(); Check(outputPower.linked == null && inputPower.linked == null && outputPower.targetPowerOutput == 0, "power link undo restores both ends and output configuration");
            Map submap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)); submap.info = new MapInfo { Size = new IntVec3(20, 1, 20), parent = (MapParent_Custom)RuntimeHelpers.GetUninitializedObject(typeof(MapParent_Custom)) }; submap.uniqueID = 792; submap.components = new(); submap.listerThings = new ListerThings(ListerThingsUse.Global); submap.mapPawns = new MapPawns(submap); game.Maps.Add(submap);
            submap.cellIndices = new CellIndices(submap); submap.designationManager = new DesignationManager(submap);
            CustomMapEntrance entrance = Thing<CustomMapEntrance>("CQF_Check_RuntimeEntrance"); CustomMapExit exit = Thing<CustomMapExit>("CQF_Check_RuntimeExit"); exit.exitName = "CQF_Check_RuntimeExitName";
            map.listerThings.AllThings.Remove(exit); submap.listerThings.AllThings.Add(exit); typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(exit, (sbyte)1);
            registry = Registry(true); Operate("<kind>link_portals</kind><entrance_id>" + entrance.ThingID + "</entrance_id><exit_id>" + exit.ThingID + "</exit_id>");
            Check(entrance.CustomMap == submap && entrance.exit == exit && exit.entrance == entrance && ((MapParent_Custom)submap.Parent).entrance == entrance && ((MapParent_Custom)submap.Parent).exit == exit, "portal linking updates the actual entrance exit destination and map parent");
            registry.RuntimeJournal.Undo(); Check(entrance.CustomMap == null && entrance.exit == null && exit.entrance == null && ((MapParent_Custom)submap.Parent).exit == null, "portal undo restores both objects and map parent references");
            registry = Registry(true); Operate("<kind>world_map_configure</kind><map_id>792</map_id><level>3</level><permanent>true</permanent>");
            Check(((MapParent_Custom)submap.Parent).level == 3 && ((MapParent_Custom)submap.Parent).permanent, "world metadata editing updates the real custom map parent");
            registry.RuntimeJournal.Undo(); Check(((MapParent_Custom)submap.Parent).level == 0 && !((MapParent_Custom)submap.Parent).permanent, "world map metadata undo restores its original values");
            Check(((MapParent_Custom)submap.Parent).sourceMap == null, "portal undo also restores the prior pocket-map ownership reference");
            DialogTreeDef refreshed = (DialogTreeDef)model.Copy(dialog); refreshed.label = "CQF_Check_Refreshed";
            CQFQuestDefBootstrap.HotLoadDefinition(refreshed); trigger.Props.dialog = dialog;
            int rebound = new CQFAIDefinitionBindings().Refresh(refreshed);
            Check(rebound > 0 && trigger.Props.dialog == refreshed, "definition refresh reconnects actual stale component bindings to the current loaded Def");
            registry = Registry(true);
            Operate("<kind>map_queues_configure</kind><map_id>791</map_id><changes_xml><![CDATA[<changes><put path='/routes'><key>CQF_Check_Route</key><value><li>2,0,3</li><li>4,0,5</li></value></put></changes>]]></changes_xml>");
            Check(mapData.route["CQF_Check_Route"].route.Last() == new IntVec3(4, 0, 5), "route operations update actual CQF map paths without spawning NPCs");
            registry.RuntimeJournal.Undo(); Check(mapData.route.Count == 0, "route undo restores prior configuration");

            QuestBookInstance book = new() { instanceId = "CQF_Check_RuntimeBook", state = QuestBookState.Active, bookDef = new QuestBookDef { defName = "CQF_Check_RuntimeBookDef", chapters = new() { new QuestBookChapter { id = "CQF_Check_Chapter", steps = new() { new QuestBookStep { id = "CQF_Check_Step" } } } } }, steps = new() { new QuestBookStepState { stepId = "CQF_Check_Step", chapterId = "CQF_Check_Chapter", status = QuestBookStepStatus.Active, objectives = new() { new QuestBookObjectiveProgress { currentCount = 2 } } } } };
            books.Instances.Add(book); registry = Registry(true);
            Operate("<kind>quest_objective</kind><instance_id>" + book.instanceId + "</instance_id><step_id>CQF_Check_Step</step_id><index>0</index><count>5</count><completed>true</completed>");
            Check(book.steps[0].objectives[0].currentCount == 5 && book.steps[0].status == QuestBookStepStatus.Active, "objective editing updates progress without implicitly completing steps or issuing rewards");
            registry.RuntimeJournal.Undo(); Check(book.steps[0].objectives[0].currentCount == 2 && !book.steps[0].objectives[0].completed, "objective undo restores previous actual progress");
            Check(Read("<kind>quest_steps</kind><instance_id>" + book.instanceId + "</instance_id>").Descendants("step").Single().Attribute("status")?.Value == "Active", "QuestBook query reads actual running step status");
            book.steps[0].objectives.AddRange(Enumerable.Range(0, 100).Select(index => new QuestBookObjectiveProgress { currentCount = index }));
            Check(!Read("<kind>quest_steps</kind><instance_id>" + book.instanceId + "</instance_id>").Descendants("objective").Any(), "step discovery summarizes large objective lists");
            XElement objectivePage = Read("<kind>quest_objectives</kind><instance_id>" + book.instanceId + "</instance_id><step_id>CQF_Check_Step</step_id><offset>25</offset><limit>2</limit>");
            Check(objectivePage.Descendants("objective").Count() == 2 && objectivePage.Descendants("objective").First().Attribute("index")?.Value == "25", "actual objective readback is paginated with stable source indices");
            Quest actualQuest = new() { id = 713 }; actualQuest.PartsListForReading.Add(new QuestPart_QuestBookBinding { instanceId = book.instanceId, bookDef = book.bookDef }); game.questManager.QuestsListForReading.Add(actualQuest);
            Check(Read("<kind>quest_parts</kind><quest_id>713</quest_id>").Descendants("part").Single().Attribute("fieldCount") != null, "running Quest part discovery exposes field counts without dumping runtime object graphs");
            XElement partFields = Read("<kind>quest_part_fields</kind><quest_id>713</quest_id><part_index>0</part_index><limit>1</limit>");
            Check(partFields.Descendants("field").Count() == 1 && partFields.Descendants("fields").Single().Attribute("hasMore")?.Value == "true", "actual Quest part fields support bounded follow-up pagination");
            Reject(() => Read("<kind>quest_part_fields</kind><quest_id>713</quest_id><part_index>1</part_index>"), "Quest part reads require an exact existing index");
            Reject(() => CQFAIPawnRuntime.ValidateSpawnData(new PawnSpawnData_Random { datas = new() { new PawnSpawnData { kind = kind, count = new IntRange(1, 41) } } }), "random Pawn recipes cannot hide oversized child counts");

            CQFCheckRuntimeHolder holder = Thing<CQFCheckRuntimeHolder>("CQF_Check_RuntimeHolder"), destination = Thing<CQFCheckRuntimeHolder>("CQF_Check_RuntimeDestination");
            holder.contents = new ThingOwner<Thing>(holder); destination.contents = new ThingOwner<Thing>(destination);
            ThingDef itemDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); itemDef.defName = "CQF_Check_RuntimeItem"; itemDef.label = itemDef.defName; itemDef.thingClass = typeof(Thing); itemDef.category = ThingCategory.Item; itemDef.stackLimit = 75; DefDatabase<ThingDef>.Add(itemDef);
            registry = Registry(true);
            Operate("<kind>inventory_add</kind><thing_id>" + holder.ThingID + "</thing_id><definition>" + itemDef.defName + "</definition><count>20</count>");
            Thing held = holder.contents.Single(); Check(held.stackCount == 20 && held.holdingOwner == holder.contents && !held.Spawned, "inventory creation adds an actual held stack without map spawning");
            Operate("<kind>inventory_count</kind><thing_id>" + holder.ThingID + "</thing_id><item_id>" + held.ThingID + "</item_id><count>30</count>");
            Operate("<kind>inventory_transfer</kind><thing_id>" + holder.ThingID + "</thing_id><item_id>" + held.ThingID + "</item_id><destination_id>" + destination.ThingID + "</destination_id>");
            Check(holder.contents.Count == 0 && ReferenceEquals(destination.contents.Single(), held) && held.stackCount == 30, "inventory transfer moves the same actual stack and preserves its data");
            registry.RuntimeJournal.Undo(); Check(holder.contents.Count == 0 && destination.contents.Count == 0 && held.stackCount == 20, "mixed add/count/transfer task undo restores both inventories");
            Check(holder.contents.TryAdd(held, false), "inventory fixture restores the retained item for removal checks");
            registry = Registry(true); Operate("<kind>inventory_remove</kind><thing_id>" + holder.ThingID + "</thing_id><item_id>" + held.ThingID + "</item_id>");
            Check(holder.contents.Count == 0 && !held.Destroyed, "inventory remove retains the original instance for undo rather than destroying it");
            registry.RuntimeJournal.Undo(); Check(ReferenceEquals(holder.contents.Single(), held), "inventory remove undo restores the exact held instance");
            XElement heldQuery = Read("<kind>inventory</kind><thing_id>" + holder.ThingID + "</thing_id><limit>1</limit>");
            Check(heldQuery.Descendants("item").Single().Attribute("id")?.Value == held.ThingID, "inventory query returns actual held IDs for follow-up operations");
            Reject(() => Operate("<kind>inventory_count</kind><thing_id>" + holder.ThingID + "</thing_id><item_id>" + held.ThingID + "</item_id><count>76</count>"), "inventory count validates actual definition stack limits");

            registry = Registry(true); SignalReceiver receiver = new(); game.signalManager.receivers.Add(receiver);
            Operate("<kind>send_signal</kind><tag>CQF_Check_RealSignal</tag><targets><target><key>CustomThing</key><thing_id>" + ordinary.ThingID + "</thing_id></target></targets>");
            Check(receiver.Last?.tag == "CQF_Check_RealSignal" && receiver.Last?.args.Args.Single().arg == ordinary, "signal operation dispatches to real registered receivers with typed Thing arguments");
            Check(!registry.RuntimeJournal.UndoSupported && !registry.RuntimeJournal.CanUndo, "signal dispatch marks actual game effects as not fully undoable");
            registry = Registry(true); CQFCheckRuntimeAction.Invocations = 0;
            XElement invoked = Operate("<kind>run_actions</kind><actions_xml><![CDATA[<value><li Class='CQFCheckRuntimeAction'/><li Class='CQFCheckRuntimeAction'><throwError>true</throwError></li><li Class='CQFCheckRuntimeAction'/></value>]]></actions_xml>");
            Check(invoked.Attribute("success")?.Value == "false" && CQFCheckRuntimeAction.Invocations == 1 && invoked.Descendants("actionsInvoked").Single().Attribute("partial")?.Value == "true", "partial CQF action failure is reported and subsequent actions stop");
            XElement logged = Operate("<kind>run_actions</kind><actions_xml><![CDATA[<value><li Class='CQFCheckRuntimeAction'><logError>true</logError></li></value>]]></actions_xml>");
            Check(logged.Attribute("success")?.Value == "false" && logged.Descendants("runtimeErrors").Any(), "game error logs without exceptions still become failed tool results");
            int priorInvocations = CQFCheckRuntimeAction.Invocations;
            Reject(() => Operate("<kind>run_actions</kind><actions_xml><![CDATA[<value><li Class='QuestEditor_Library.CQFAction_Loop'><loopCount>128</loopCount><actions><li Class='QuestEditor_Library.CQFAction_Loop'><loopCount>128</loopCount><actions><li Class='CQFCheckRuntimeAction'/></actions></li></actions></li></value>]]></actions_xml>"), "nested loops cannot multiply the runtime action budget beyond the bound");
            Check(CQFCheckRuntimeAction.Invocations == priorInvocations, "execution budget rejection happens before any actions run");
            CQFAIToolRegistry textRestricted = new(model, resources, null, null, "", false, true);
            Reject(() => textRestricted.Execute(new CQFAIToolCall("CQF_Check_TextPermission", "cqf_operate_runtime", new XElement("arguments", new XElement("operation_xml", "<operation><kind>run_actions</kind><actions_xml><![CDATA[<value><li Class='QuestEditor_Library.CQFAction_Message'><message>Unsupplied narrative</message></li></value>]]></actions_xml></operation>")))), "runtime actions respect disabled prose generation");
            textRestricted.AddInstruction("Use exactly this supplied label: Supplied after steering");
            XElement steered = textRestricted.Execute(new CQFAIToolCall("CQF_Check_SteeredPermission", "cqf_manage_definition", new XElement("arguments", new XElement("request_xml", "<request><kind>create</kind><type>QuestEditor_Library.DialogTreeDef</type><name>CQF_Check_SteeredDefinition</name><changes_xml><![CDATA[<changes><set path='/label'><value>Supplied after steering</value></set></changes>]]></changes_xml></request>"))));
            Check(steered.Attribute("success")?.Value == "true" && DefDatabase<DialogTreeDef>.GetNamed("CQF_Check_SteeredDefinition").label == "Supplied after steering", "mid-task supplied text reaches runtime definition tools without enabling prose generation");
            XElement conditions = Call("cqf_check_conditions", new XElement("request_xml", "<request><conditions_xml><![CDATA[<value><li Class='CQFCheckRuntimeCondition'><result>true</result></li><li Class='CQFCheckRuntimeCondition'/></value>]]></conditions_xml></request>"));
            Check(conditions.Descendants("condition").Count() == 2 && conditions.Descendants("reason").Last().Value == "CQF_Check_ConditionFailed", "condition tool returns per-condition actual results and failure reasons");

            registry = Registry(true); string name = "CQF_Check_RuntimeCreatedDialogue";
            XElement created = Manage("<kind>create</kind><type>QuestEditor_Library.DialogTreeDef</type><name>" + name + "</name>");
            Check(DefDatabase<DialogTreeDef>.GetNamed(name).defName == name && created.Descendants("definitionManaged").Single().Attribute("saved")?.Value == "false", "new definition is registered and discoverable without unrequested source writes");
            Manage("<kind>save</kind><type>QuestEditor_Library.DialogTreeDef</type><name>" + name + "</name>");
            string path = Path.Combine(directory, "Quests", "DialogTree", name + ".xml"); Check(File.Exists(path) && XDocument.Load(path).Root?.Name == "Defs", "save operation writes valid source XML in the framework bootstrap category");
            string beforeFile = File.ReadAllText(path);
            Reject(() => Manage("<kind>save</kind><type>QuestEditor_Library.DialogTreeDef</type><name>" + name + "</name>"), "source overwrite requires explicit instruction");
            Check(File.ReadAllText(path) == beforeFile, "rejected save leaves the source file intact");
            Manage("<kind>copy</kind><type>QuestEditor_Library.DialogTreeDef</type><name>CQF_Check_RuntimeCopiedDialogue</name><source_name>" + name + "</source_name>");
            Check(!ReferenceEquals(DefDatabase<DialogTreeDef>.GetNamed(name).nodeMoulds, DefDatabase<DialogTreeDef>.GetNamed("CQF_Check_RuntimeCopiedDialogue").nodeMoulds), "definition copy owns independent editable node data");
            XElement exported = Call("cqf_export_definition", new XElement("type", "QuestEditor_Library.DialogTreeDef"), new XElement("name", name), new XElement("limit", "50"));
            Check(exported.Element("definitionXml")?.Value.Length == 50 && (int)exported.Element("definitionXml")!.Attribute("totalCharacters")! > 50, "definition export uses bounded character pages");
            Reject(() => Manage("<kind>create</kind><type>QuestEditor_Library.DialogTreeDef</type><name>../Bad</name>"), "new definitions reject path-like names before mutation");
            Reject(() => Manage("<kind>create</kind><type>QuestEditor_Library.DialogTreeDef</type><name>NoPrefix</name>"), "new definition names require the CQF prefix");

            registry = Registry(true); int scalar = 1; registry.RuntimeJournal.Edit("CQF_Check_Journal", () => scalar = 2, () => scalar = 1, () => new XElement("value", scalar));
            Game otherGame = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game)); Current.Game = otherGame;
            Check(!registry.RuntimeJournal.IsTargetValid, "runtime journals detect active game replacement"); Reject(registry.RuntimeJournal.Undo, "task undo cannot cross games"); Current.Game = game;
            CQFAITaskTransaction aggregate = new(model); aggregate.Add(registry.RuntimeJournal); registry.RuntimeJournal.MarkIrreversible();
            Check(!aggregate.CanUndo && !aggregate.UndoSupported, "an irreversible operation disables whole-task undo instead of offering partial recovery");
            CQFAIRuntimeJournal bounded = new(model); string largeValue = "before";
            XElement boundedReceipt = bounded.Edit("CQF_Check_BoundedReceipt", () => largeValue = new string('x', 20000), () => largeValue = "before", () => new XElement("state", largeValue));
            Check(boundedReceipt.Descendants("receipt").Single().Attribute("truncated")?.Value == "true" && bounded.IsCurrent, "runtime mutation receipts stay bounded while undo fingerprints retain full state");
            bounded.Undo(); Check(largeValue == "before", "bounded receipts preserve full mutation undo");
            CQFAITaskState verification = new(); verification.RecordTool("cqf_operate_runtime", false);
            Check(verification.RequiresVerification, "failed runtime operations require state inspection before goal completion");
            verification.RecordTool("cqf_runtime_help", true); Check(verification.RequiresVerification, "help discovery cannot substitute for actual post-failure state verification");
            verification.RecordTool("cqf_read_runtime", true); Check(!verification.RequiresVerification, "actual runtime readback satisfies the task verification checkpoint");
            CQFAIOperation malformed = new(new CQFAIToolCall("CQF_Check_bad", "cqf_operate_runtime", new XElement("arguments", new XElement("operation_xml", "<"))));
            Check(malformed.Details.Length > 0, "malformed operation preview preserves the error without crashing progress UI");

            CQFAIToolRegistry Registry(bool editing) => new(model, resources, null, null, "", true, editing);
            XElement Call(string tool, params XElement[] arguments) => registry.Execute(new CQFAIToolCall("CQF_Check_" + Guid.NewGuid().ToString("N"), tool, new XElement("arguments", arguments)));
            XElement Operate(string body) => Call("cqf_operate_runtime", new XElement("operation_xml", "<operation>" + body + "</operation>"));
            XElement Read(string body) => Call("cqf_read_runtime", new XElement("query_xml", "<query>" + body + "</query>"));
            XElement Manage(string body) => Call("cqf_manage_definition", new XElement("request_xml", "<request>" + body + "</request>"));
            T Thing<T>(string name) where T : Thing
            {
                T thing = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
                ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = name; def.label = name; def.thingClass = typeof(T); def.category = typeof(T) == typeof(Pawn) ? ThingCategory.Pawn : ThingCategory.Building; def.size = new IntVec2(1, 1); def.stackLimit = 1; thing.def = def;
                thing.stackCount = 1; thing.thingIDNumber = map.listerThings.AllThings.Count + 1;
                typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(thing, (sbyte)0); map.listerThings.AllThings.Add(thing);
                return thing;
            }
        }
        finally
        {
            Current.Game = oldGame!; GameComponent_Editor.Instance = oldEditor!; GameComponent_ComplexDuty.Instance = oldDuty!; GameComponent_QuestBook.Instance = oldBook!; rootField.SetValue(null, oldRoot);
            mod.assemblies.loadedAssemblies.Remove(typeof(CQFCheckRuntimeAction).Assembly); GenTypes.ClearCache();
            string full = Path.GetFullPath(directory), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            if (Path.GetDirectoryName(full) != temp || !Path.GetFileName(full).StartsWith("CQF_Runtime_Checks_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected runtime test directory");
            Directory.Delete(full, true);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); Console.WriteLine("PASS " + message); }
    private static void Reject(Action action, string message)
    { try { action(); } catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException) { Console.WriteLine("PASS " + message); return; } throw new InvalidOperationException("Expected rejection: " + message); }
    private sealed class SignalReceiver : ISignalReceiver
    {
        public void Notify_SignalReceived(Signal signal) { Last = signal; }
        public Signal? Last;
    }
}
