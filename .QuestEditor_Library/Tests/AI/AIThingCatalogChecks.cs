using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIThingCatalogChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        ModContentPack external = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        typeof(ModContentPack).GetField("packageIdInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(external, "CQF.Check.External");
        ModContentPack owned = LoadedModManager.RunningModsListForReading.First(mod => mod.assemblies.loadedAssemblies.Contains(typeof(InteractableThing).Assembly));
        ThingDef Create(string name, Type type, ModContentPack mod)
        {
            ThingDef def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name; def.label = name; def.thingClass = type; def.modContentPack = mod; def.size = new IntVec2(1, 1); def.stackLimit = 1;
            def.category = ThingCategory.Building; def.comps = new List<CompProperties>();
            return def;
        }
        List<ThingDef> fixtures = new();
        foreach (var entry in new (string kind, Type type)[]
        {
            ("interaction", typeof(InteractableThing)), ("entrance", typeof(CQFCheckDerivedEntrance)), ("exit", typeof(CustomMapExit)),
            ("loot", typeof(LootBox)), ("trap", typeof(CustomTrap_Capture)), ("container", typeof(CustomContainer)),
            ("door", typeof(CustomDoor)), ("spawner", typeof(QuestEditor_Library.Spawner)), ("zone", typeof(ZoneCore)), ("generation", typeof(GenerationActionWorker))
        })
        {
            ThingDef definition = Create("CQF_Check_Catalog_" + entry.kind, entry.type, external);
            definition.label = "CQF_Check_标签_" + entry.kind;
            DefDatabase<ThingDef>.Add(definition); fixtures.Add(definition);
            XElement details = CQFAIThingCatalog.Describe(definition)!;
            Check(details.Attribute("kind")?.Value == entry.kind && (bool)details.Attribute("frameworkOwned")! == false, "CQF capability recognizes external subclasses without requiring framework ownership: " + entry.kind);
            Check((bool)details.Attribute("liveFeatureEditing")!, "catalog reports actual live feature editing scope: " + entry.kind);
            Check(model.Types.Any(type => type.FullName == details.Element("liveConfiguration")?.Attribute("type")?.Value), "catalog exposes a supported live schema and configuration path: " + entry.kind);
            if (entry.kind is not ("spawner" or "generation")) Check(model.Types.Any(type => type.FullName == details.Element("draftDataType")?.Value), "catalog draft configuration type exists in the actual schema registry: " + entry.kind);
        }
        ThingDef worker = Create("CQF_Check_Catalog_Component", typeof(ThingWithComps), external);
        worker.comps.Add(new CompProperties(typeof(CQFCheckDerivedActionWorker)));
        DefDatabase<ThingDef>.Add(worker); fixtures.Add(worker);
        Check(CQFAIThingCatalog.Describe(worker)?.Attribute("kind")?.Value == "action_worker", "CQF functionality is recognized from inherited action-worker components on ordinary runtime classes");
        ThingDef pawn = Create("CQF_Check_Catalog_Pawn", typeof(Pawn), external); pawn.comps.Add(new CompProperties(typeof(CQFCheckDerivedActionWorker)));
        Check(CQFAIThingCatalog.Describe(pawn)?.Attribute("liveFeatureEditing")?.Value == "false"
            && !CQFAIThingCatalog.Describe(pawn)!.Elements("liveTool").Any(tool => tool.Value == "cqf_edit_map_thing"), "discovery does not claim deferred Pawn live editing through action-worker components");
        ThingDef ordinary = Create("CQF_Check_Catalog_PrefixOnly", typeof(Thing), external);
        DefDatabase<ThingDef>.Add(ordinary); fixtures.Add(ordinary);
        Check(CQFAIThingCatalog.Describe(ordinary) == null, "a CQF-looking name alone does not invent framework functionality");
        ThingDef coreResource = Create("CQF_Check_Catalog_CoreResource", typeof(Thing), owned);
        DefDatabase<ThingDef>.Add(coreResource); fixtures.Add(coreResource);
        Check(CQFAIThingCatalog.Describe(coreResource)?.Attribute("kind")?.Value == "resource", "framework-owned ordinary resources remain discoverable alongside functional objects");
        XElement listed = CQFAIThingCatalog.Query("entrance", "CQF_Check_标签", 0);
        Check((int)listed.Attribute("total")! == 1 && listed.Elements("def").Single().Attribute("mod")?.Value == external.PackageId, "function discovery supports localized labels and exact actual source Mod IDs");
        Check(CQFAIThingCatalog.Query("entrance", "CQFCheckDerivedEntrance", 0).Elements("def").Single().Attribute("name")?.Value == "CQF_Check_Catalog_entrance", "runtime class discovery finds a submod's inherited entrance type");
        XElement generic = catalog.Query(XElement.Parse("<queries><defs type='Verse.ThingDef' search='CQF_Check_Catalog_entrance'/></queries>"));
        Check(generic.Descendants("cqf").Single().Attribute("kind")?.Value == "entrance" && generic.Descendants("placement").Single().Attribute("thingClass")?.Value == typeof(CQFCheckDerivedEntrance).FullName,
            "ordinary Def queries also expose actual CQF capability and runtime class metadata");
        Check(catalog.Summary().Descendants("cqfThings").Single().Attribute("discoveryTool")?.Value == "cqf_list_cqf_things" && catalog.Summary().Descendants("kind").Any(k => k.Attribute("name")?.Value == "entrance"),
            "initial resource summary advertises actual available CQF kinds and discovery tool");
        CQFAIToolRegistry registry = new(model, catalog, null, null, "", false);
        registry.RestrictToInspection();
        XElement result = registry.Execute(new CQFAIToolCall("CQF_Check_Catalog_Tool", "cqf_list_cqf_things", new XElement("arguments", new XElement("kind", "loot"), new XElement("search", "CQF_Check_Catalog"))));
        Check(result.Descendants("cqf").Single().Attribute("kind")?.Value == "loot" && !registry.Tools.Any(t => t.Name == "cqf_apply_changes"), "read-only agents can discover CQF functionality without acquiring write tools");
        string instructions = new CQFAIConversation(model, catalog).Instructions(null, true, true, registry, true, true);
        Check(instructions.Contains("proactively use cqf_list_cqf_things") && instructions.Contains("liveFeatureEditing") && instructions.Contains("Do not add CQF features to unrelated requests"),
            "instructions guide relevant CQF discovery while preserving capability limits and unrelated requests");
        for (int index = 0; index < 22; index++)
        {
            ThingDef def = Create("CQF_Check_Catalog_Page_" + index.ToString("00"), typeof(LootBox), external);
            DefDatabase<ThingDef>.Add(def); fixtures.Add(def);
        }
        Check(CQFAIThingCatalog.Query("loot", "CQF_Check_Catalog_Page", 0).Elements("def").Count() == 20 && CQFAIThingCatalog.Query("loot", "CQF_Check_Catalog_Page", 20).Elements("def").Count() == 2,
            "CQF discovery is paginated rather than injecting every custom definition into the prompt");
        Reject(() => CQFAIThingCatalog.Query("unknown", "", 0), "unknown CQF functionality filters return actionable errors");
        Reject(() => CQFAIThingCatalog.Query("", "", -1), "CQF discovery rejects invalid offsets");
        Reject(() => CQFAIThingCatalog.Query("", new string('x', 101), 0), "CQF discovery bounds search input");
        MethodInfo remove = typeof(DefDatabase<ThingDef>).GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (ThingDef definition in fixtures) remove.Invoke(null, new object[] { definition });
    }
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
