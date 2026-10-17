using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class HibernationSaveMigrationChecks
{
    public static void Run(Game game, string fixtureSavePath)
    {
        XElement saved = XDocument.Load(fixtureSavePath).Root!.Element("manager")!
            .Element("CQF_ownedHibernatingMaps")!.Elements("li").Single();
        XElement reference = new("CQF_hibernatingMaps", new XElement("li", "Map_1"));
        XElement pause = new("CQF_mapPauses", new XElement("li",
            new XElement("map", "Map_1"), new XElement("regionUpdaterEnabled", false)));
        CheckCase(game, new XElement("manager", reference), new[] { 1 }, true, false, "3.4/3.5 reference list");
        CheckCase(game, new XElement("manager", pause), new[] { 1 }, false, false, "reference pause record");
        CheckCase(game, new XElement("manager", new XElement("CQF_hibernatingMaps", new XElement(saved))),
            new[] { 2 }, true, false, "intermediate owned-map node");
        XElement typed = new(saved);
        typed.SetAttributeValue("Class", "QuestEditor_Library.CQFHibernatingMap");
        CheckCase(game, new XElement("manager", new XElement("CQF_hibernatingMaps", typed)),
            new[] { 2 }, true, false, "typed intermediate owned-map node");
        XElement linked = new(saved);
        linked.Element("map")!.SetElementValue("CQF_Check_source", "Map_1");
        XElement mixed = new("CQF_hibernatingMaps", new XElement("li", "Map_1"), linked);
        CheckCase(game, new XElement("manager", mixed, pause,
            new XElement("CQF_pendingHibernation", new XElement("li", "Map_1"))),
            new[] { 1, 2 }, false, true, "mixed old formats and duplicate pause requests");
        CheckCase(game, new XElement("manager", reference,
            new XElement("CQF_ownedHibernatingMaps", linked)),
            new[] { 1, 2 }, true, true, "legacy reference plus current owned map");
        string? supplied = Environment.GetEnvironmentVariable("CQF_CHECKS_LEGACY_SAVE");
        if (!string.IsNullOrEmpty(supplied))
        {
            XElement manager = XDocument.Load(supplied).Descendants("li").Single(element =>
                element.Attribute("Class")?.Value == "QuestEditor_Library.GameComponent_MapManager");
            CheckCase(game, new XElement("manager", manager.Elements().Select(element => new XElement(element))),
                new[] { 1 }, true, false, "supplied pre-update save manager");
        }
        Map[] before = game.Maps.ToArray();
        string invalid = WriteFixture(new XElement("manager", new XElement("CQF_ownedHibernatingMaps", new XElement("li"))));
        GameComponent_MapManager malformed = ReadManager(game, invalid);
        bool rejected = false;
        try { malformed.LoadedGame(); }
        catch (InvalidOperationException error) { rejected = error.Message.Contains("CQF_ownedHibernatingMaps"); }
        finally { malformed.DisposeMaps(); }
        Check(rejected && game.Maps.SequenceEqual(before), "malformed owned-map data fails before changing active map ownership");
        links.Clear();
    }

    internal static void ExposeFixtureLink(Map map)
    {
        links.TryGetValue(map, out Map? source);
        Scribe_References.Look(ref source, "CQF_Check_source");
        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && source != null) links[map] = source;
    }

    private static void CheckCase(Game game, XElement manager, int[] expected, bool updater, bool linked, string name)
    {
        Map[] before = game.Maps.ToArray();
        bool[] updaterFlags = before.Select(map => map.regionAndRoomUpdater.Enabled).ToArray();
        string input = WriteFixture(manager);
        GameComponent_MapManager loaded = ReadManager(game, input);
        try
        {
            loaded.LoadedGame();
            Check(loaded.HibernatingMaps.Select(map => map.uniqueID).OrderBy(id => id).SequenceEqual(expected)
                && loaded.HibernatingMaps.All(map => !game.Maps.Contains(map)), name + " preserves every map and restores dormancy");
            if (expected.Contains(1))
                Check(ReferenceEquals(loaded.HibernatingMaps.Single(map => map.uniqueID == 1), before.Single(map => map.uniqueID == 1)),
                    name + " reuses the native map object instead of regenerating it");
            if (expected.Contains(2))
            {
                Map restored = loaded.HibernatingMaps.Single(map => map.uniqueID == 2);
                Check(restored.spawnedThings.Count == 3 && restored.spawnedThings.All(thing => !thing.Spawned && thing.Map == null)
                    && restored.spawnedThings.OfType<PauseProbeThing>().Single(probe => probe.def.tickerType == TickerType.Normal).Ticks == 2110,
                    name + " preserves unspawned things and their saved state");
            }
            if (linked)
            {
                Map root = loaded.HibernatingMaps.Single(map => map.uniqueID == 1);
                Map child = loaded.HibernatingMaps.Single(map => map.uniqueID == 2);
                Check(links.TryGetValue(child, out Map? source) && ReferenceEquals(source, root),
                    name + " retains cross-map reference identity");
            }
            string converted = Path.Combine(Path.GetTempPath(), "CQF_MigratedSave_" + Guid.NewGuid().ToString("N") + ".xml");
            Scribe.saver.InitSaving(converted, "checks");
            Scribe_Deep.Look(ref loaded, "manager", game);
            foreach (Map map in game.Maps) Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(map, "externalMapFixture");
            Scribe.saver.FinalizeSaving();
            XDocument xml = XDocument.Load(converted);
            Check(xml.Descendants("CQF_ownedHibernatingMaps").Elements("li").Count() == expected.Length
                && !xml.Descendants("CQF_hibernatingMaps").Any() && !xml.Descendants("CQF_mapPauses").Any(),
                name + " saves only the current owned-map format without dropping maps");
            CheckConvertedSave(game, converted, expected, updater, linked, name);
            if (expected.Contains(1))
            {
                Map root = loaded.HibernatingMaps.Single(map => map.uniqueID == 1);
                loaded.Activate(root);
                Check(root.regionAndRoomUpdater.Enabled == updater, name + " preserves the original region updater flag");
            }
        }
        finally
        {
            ReleaseFixtureManager(game, loaded, before);
            for (int i = 0; i < before.Length; i++) before[i].regionAndRoomUpdater.Enabled = updaterFlags[i];
        }
        Check(game.Maps.SequenceEqual(before), name + " leaves the test world's active maps intact");
    }

    private static void CheckConvertedSave(Game original, string file, int[] expected, bool updater, bool linked, string name)
    {
        Game clone = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        clone.components = new();
        clone.currentMapIndex = -1;
        clone.tickManager = new TickManager();
        typeof(Game).GetField("worldInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(clone, original.World);
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(clone, new List<Map>());
        Current.Game = clone;
        GameComponent_MapManager? loaded = null;
        try
        {
            loaded = ReadManager(clone, file);
            clone.components.Add(loaded);
            loaded.LoadedGame();
            Check(clone.Maps.Count == 0 && loaded.HibernatingMaps.Select(map => map.uniqueID).OrderBy(id => id).SequenceEqual(expected),
                name + " reloads after conversion with all owned maps present");
            if (linked)
            {
                Map child = loaded.HibernatingMaps.Single(map => map.uniqueID == 2);
                Check(links.TryGetValue(child, out Map? source)
                    && ReferenceEquals(source, loaded.HibernatingMaps.Single(map => map.uniqueID == 1)),
                    name + " retains cross-map references after a second save/load");
            }
            if (expected.Contains(1))
            {
                Map root = loaded.HibernatingMaps.Single(map => map.uniqueID == 1);
                loaded.Activate(root);
                Check(root.regionAndRoomUpdater.Enabled == updater, name + " retains the updater flag after a second save/load");
            }
        }
        finally
        {
            if (loaded != null) ReleaseFixtureManager(clone, loaded, Array.Empty<Map>());
            Current.Game = original;
        }
    }

    private static GameComponent_MapManager ReadManager(Game game, string file)
    {
        Scribe.loader.InitLoading(file);
        LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
            .GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
        foreach (Map map in game.Maps) directory.RegisterLoaded(map);
        GameComponent_MapManager loaded = null!;
        Scribe_Deep.Look(ref loaded, "manager", game);
        Scribe.loader.FinalizeLoading();
        return loaded;
    }

    private static void ReleaseFixtureManager(Game game, GameComponent_MapManager loaded, Map[] original)
    {
        foreach (Map map in loaded.HibernatingMaps.ToArray()) loaded.Activate(map);
        Map[] created = game.Maps.Where(map => !original.Contains(map)).ToArray();
        foreach (Map map in created)
            if (map.spawnedThings.Count > 0) Find.TickManager.RemoveAllFromMap(map);
        foreach (Map map in created)
        {
            foreach (Thing thing in map.spawnedThings) thing.ForceSetStateToUnspawned();
            game.Maps.Remove(map);
        }
        loaded.DisposeMaps();
    }

    private static string WriteFixture(XElement manager)
    {
        string file = Path.Combine(Path.GetTempPath(), "CQF_MigrationFixture_" + Guid.NewGuid().ToString("N") + ".xml");
        new XDocument(new XElement("checks", manager)).Save(file);
        return file;
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private static readonly Dictionary<Map, Map> links = new();
}
