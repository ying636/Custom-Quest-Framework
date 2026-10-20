using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using HarmonyLib;
using HCFWithCQF.Actions;
using HiveCreatureFramework;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class Program
{
    private static void Main()
    {
        try
        {
            new Harmony("CQF.Checks.HCF").Patch(typeof(Log).GetMethod(nameof(Log.Error)),
                prefix: new HarmonyMethod(typeof(Program).GetMethod(nameof(RecordError), BindingFlags.NonPublic | BindingFlags.Static)));
            FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static);
            prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
            string modRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            LoadedLanguage language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.info = new LanguageInfo();
            typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(language, true);
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement>();
            foreach (string folder in new[] { Path.Combine(modRoot, "Languages", "English", "Keyed"), Path.Combine(modRoot, "..", "巢群生物框架", "Languages", "English", "Keyed") })
                foreach (string path in Directory.EnumerateFiles(folder, "*.xml"))
                    foreach (XElement entry in XDocument.Load(path).Root.Elements())
                        language.keyedReplacements[entry.Name.LocalName] = new LoadedLanguage.KeyedReplacement { value = entry.Value };
            LanguageDatabase.activeLanguage = language;
            LanguageDatabase.defaultLanguage = language;
            ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
            content.assemblies = new ModAssemblyHandler(content);
            content.assemblies.loadedAssemblies.Add(typeof(CQFAction).Assembly);
            content.assemblies.loadedAssemblies.Add(typeof(CQFAction_JoinHiveGroup).Assembly);
            content.assemblies.loadedAssemblies.Add(typeof(UnitGroup).Assembly);
            LoadedModManager.RunningModsListForReading.Add(content);
            GenTypes.ClearCache();
            new GameComponent_Editor(null);
            Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game,
                new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Current.Game = game;
            World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
            world.factionManager = new FactionManager();
            Faction player = (Faction)RuntimeHelpers.GetUninitializedObject(typeof(Faction));
            player.def = new FactionDef { defName = "CQF_TestPlayer", isPlayer = true };
            typeof(FactionManager).GetField("ofPlayer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world.factionManager, player);
            game.World = world;
            Run();
            Console.WriteLine($"HCF integration checks passed: {checks}");
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            Environment.ExitCode = 1;
        }
    }

    private static void Run()
    {
        ThingWithComps hive = MakeThing<ProbeHive>();
        CompHiveGroup component = new CompHiveGroup { parent = hive };
        hive.AllComps.Add(component);
        ProbeGroup first = new ProbeGroup { id = 2, hive = hive, unitLimit = 1, tags = new List<string> { "CQF_Test" } };
        ProbeGroup second = new ProbeGroup { id = 9, hive = hive, unitLimit = 10, tags = new List<string> { "CQF_Test" } };
        component.groups.Add(second);
        component.groups.Add(first);
        Unit pawn = MakeUnit();
        Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo> { ["Hive"] = hive, ["Pawn"] = pawn };
        CQFAction_JoinHiveGroup join = new CQFAction_JoinHiveGroup();
        join.Work(targets, null);
        Check(first.JoinCalls == 1 && second.JoinCalls == 0, "automatic join uses the first eligible group by ID");
        join.Work(targets, null);
        Check(first.JoinCalls == 1, "repeated join does not duplicate a member or reject a full existing group");
        Unit next = MakeUnit();
        targets["Pawn"] = next;
        join.Work(targets, null);
        Check(second.JoinCalls == 1, "automatic join advances past a full group");

        join.groupIndex = 0;
        Unit rejected = MakeUnit();
        targets["Pawn"] = rejected;
        ExpectError(() => join.Work(targets, null), "specific full group reports refusal");
        Check(HCFGameUtility.GetUnitComp(rejected).group == null, "capacity rejection leaves the unit unassigned");
        join.groupIndex = 1;
        join.Work(targets, null);
        Check(second.JoinCalls == 2, "explicit group index selects by ID");
        HCFGameUtility.GetUnitComp(rejected).Props.tags.Clear();
        Unit mismatch = MakeUnit();
        HCFGameUtility.GetUnitComp(mismatch).Props.tags.Clear();
        targets["Pawn"] = mismatch;
        ExpectError(() => join.Work(targets, null), "HCF tag rejection is reported");
        Check(lastError.Contains("CQF_HCF_JoinRejected") || lastError.Contains("could not join"), "join refusal contains a diagnostic");
        SetMap(mismatch, 1);
        ExpectError(() => join.Work(targets, null), "cross-map join is rejected");
        SetMap(mismatch, -1);
        ExpectError(() => join.Work(targets, null), "unspawned unit is rejected");
        targets["Pawn"] = hive;
        ExpectError(() => join.Work(targets, null), "non-pawn input is reported");
        join.groupIndex = 3;
        ExpectFailure(() => join.Work(targets, null), "out-of-range group index fails explicitly");
        join.groupIndex = -2;
        ExpectFailure(() => join.Work(targets, null), "invalid negative index fails explicitly");

        Unit node = MakeUnit();
        CompHiveGroup nodeComponent = new CompHiveGroup { parent = node };
        node.AllComps.Add(nodeComponent);
        ProbeGroup nodeGroup = new ProbeGroup { id = 15, hive = node, unitLimit = 10, tags = new List<string> { "CQF_Test" } };
        nodeComponent.groups.Add(nodeGroup);
        targets["Hive"] = node;
        targets["Pawn"] = MakeUnit();
        join.groupIndex = -1;
        join.Work(targets, null);
        Check(nodeGroup.JoinCalls == 1, "component node groups support joining");

        Unit hediffNode = MakeUnit();
        hediffNode.health = new Pawn_HealthTracker(hediffNode);
        HediffWithComps nodeHediff = new HediffWithComps { pawn = hediffNode, comps = new List<HediffComp>() };
        HediffComp_NodeUnit nodeHediffComp = new HediffComp_NodeUnit { parent = nodeHediff };
        ProbeGroup hediffGroup = new ProbeGroup { id = 20, hive = hediffNode, unitLimit = 10, tags = new List<string> { "CQF_Test" } };
        nodeHediffComp.groups.Add(hediffGroup);
        nodeHediff.comps.Add(nodeHediffComp);
        hediffNode.health.hediffSet.hediffs.Add(nodeHediff);
        targets["Hive"] = hediffNode;
        targets["Pawn"] = MakeUnit();
        join.Work(targets, null);
        Check(hediffGroup.JoinCalls == 1, "hediff node groups support joining");

        GroupWorkModeDef mode = new GroupWorkModeDef { defName = "CQF_TestMode", label = "test mode" };
        CQFAction_SetHiveGroupMode setMode = new CQFAction_SetHiveGroupMode { mode = mode };
        Dictionary<string, TargetInfo> modeTargets = new Dictionary<string, TargetInfo> { ["Hive"] = hive, ["SameHive"] = hive };
        setMode.RealWork(modeTargets, null);
        Check(first.ModeCalls == 1 && second.ModeCalls == 1, "all owned groups switch once despite duplicate targets");
        Check(!first.PlayerCommand && first.workModeDef == mode, "mode change uses the scripted HCF entry point");
        setMode.groupIndex = 0;
        setMode.RealWork(modeTargets, null);
        Check(first.ModeCalls == 2 && second.ModeCalls == 1, "mode group index selects by ID");
        setMode.groupIndex = -1;
        setMode.RealWork(new Dictionary<string, TargetInfo> { ["Unit"] = pawn }, null);
        Check(first.ModeCalls == 3, "ordinary unit resolves its current group");
        setMode.RealWork(new Dictionary<string, TargetInfo> { ["Node"] = node }, null);
        Check(nodeGroup.ModeCalls == 1, "component node mode is supported");
        setMode.RealWork(new Dictionary<string, TargetInfo> { ["Node"] = hediffNode }, null);
        Check(hediffGroup.ModeCalls == 1, "hediff node mode is supported");
        mode.requiresPawnTarget = true;
        ExpectError(() => setMode.RealWork(modeTargets, null), "unsupported mode target is reported");
        Check(first.ModeCalls == 3, "unsupported mode never reaches SetMode");
        mode.requiresPawnTarget = false;
        setMode.groupIndex = 10;
        ExpectError(() => setMode.RealWork(modeTargets, null), "invalid mode group index is reported");
        setMode.mode = null;
        ExpectFailure(() => setMode.RealWork(modeTargets, null), "missing mode fails explicitly");

        join.hiveKey = "CQF_TestHive";
        join.groupIndex = 1;
        XElement joinXml = join.SaveToXElement("li");
        Check(joinXml.Attribute("Class").Value == typeof(CQFAction_JoinHiveGroup).FullName
            && joinXml.Element("hiveKey").Value == "CQF_TestHive" && joinXml.Element("groupIndex").Value == "1", "join XML retains type, key and index");
        CQFAction_JoinHiveGroup copied = (CQFAction_JoinHiveGroup)join.Copy();
        Check(copied.hiveKey == join.hiveKey && copied.groupIndex == 1 && copied.targetsText.SequenceEqual(join.targetsText), "join editor copy preserves configuration");
        setMode.mode = mode;
        setMode.groupIndex = 0;
        XElement modeXml = setMode.SaveToXElement("li");
        Check(modeXml.Element("mode").Value == mode.defName && modeXml.Element("groupIndex").Value == "0", "mode XML retains Def and index");
        DefDatabase<GroupWorkModeDef>.Add(mode);
        CQFAction_SetHiveGroupMode modeCopy = (CQFAction_SetHiveGroupMode)setMode.Copy();
        Check(modeCopy.mode == mode && modeCopy.groupIndex == 0, "mode editor copy resolves the HCF Def");
        Check(!typeof(CQFAction).Assembly.GetReferencedAssemblies().Any(name => name.Name == "HiveCreatureFramework"), "CQF main assembly remains independent of HCF");
        Check(typeof(CQFAction_JoinHiveGroup).Assembly.GetTypes().Count(type => typeof(CQFAction).IsAssignableFrom(type) && !type.IsAbstract) == 2, "optional assembly exposes exactly the two CQF actions");
        Check(typeof(CQFAction).AllSubclassesNonAbstract().Contains(typeof(CQFAction_JoinHiveGroup))
            && typeof(CQFAction).AllSubclassesNonAbstract().Contains(typeof(CQFAction_SetHiveGroupMode)), "CQF action discovery includes both optional actions");
    }

    private static T MakeThing<T>() where T : ThingWithComps
    {
        T thing = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        thing.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        thing.def.defName = "CQF_Test" + typeof(T).Name;
        thing.def.label = "test " + typeof(T).Name;
        thing.def.thingClass = typeof(T);
        thing.def.useHitPoints = false;
        typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(thing, new List<ThingComp>());
        SetMap(thing, 0);
        return thing;
    }

    private static Unit MakeUnit()
    {
        Unit unit = MakeThing<Unit>();
        unit.Name = new NameSingle("test unit");
        unit.def.race = new RaceProperties();
        unit.AllComps.Add(new UnitComp { parent = unit, props = new UnitCompProperties { tags = new List<string> { "CQF_Test" } } });
        return unit;
    }

    private static void SetMap(Thing thing, sbyte index)
    {
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(thing, index);
    }

    private static bool RecordError(string text)
    {
        if (!expectingError)
            throw new InvalidOperationException("Unexpected error: " + text);
        errors++;
        lastError = text;
        Console.WriteLine("Expected error: " + text);
        return false;
    }

    private static void ExpectError(Action action, string name)
    {
        int before = errors;
        expectingError = true;
        try { action(); }
        finally { expectingError = false; }
        Check(errors > before, name);
    }

    private static void ExpectFailure(Action action, string name)
    {
        try { action(); }
        catch (InvalidOperationException) { Check(true, name); return; }
        throw new InvalidOperationException("Missing expected failure: " + name);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static int checks;
    private static int errors;
    private static string lastError;
    private static bool expectingError;
}
