using System.Text;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CQF.HibernationChecks;

public sealed class HibernationChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 360f) throw new TimeoutException("CQF hibernation checks timed out.");
            if (LongEventHandler.AnyEventNowOrWaiting || Current.Game == null || (stage == 0 && Current.Game.CurrentMap == null)) return;
            if (stage == 0)
            {
                root = GenFilePaths.SaveDataFolderPath;
                Check(GameComponent_MapManager.Instance != null, "component initialized");
                File.WriteAllLines(Path.Combine(root, "TranslationErrors.txt"), LanguageDatabase.activeLanguage.loadErrors, Encoding.UTF8);
                Current.Game.tickManager.CurTimeSpeed = TimeSpeed.Paused;
                home = Current.Game.CurrentMap;
                middle = GenerateMap(home);
                tail = GenerateMap(home);
                probes = new[] { "Normal", "Rare", "Long" }.Select((ticker, index) =>
                    (HibernationProbe)GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CQF_HibernationProbe" + ticker)),
                        new IntVec3(10 + index, 0, 10), middle)).ToArray();
                tailProbe = (HibernationProbe)GenSpawn.Spawn(ThingMaker.MakeThing(probes[0].def), new IntVec3(10, 0, 10), tail);
                Pawn inventoryPawn = home.mapPawns.FreeColonistsSpawned.First();
                inventoryPawn.DeSpawn();
                GenSpawn.Spawn(inventoryPawn, new IntVec3(20, 0, 20), middle);
                HibernationProbe inventoryProbe = (HibernationProbe)ThingMaker.MakeThing(probes[0].def);
                Check(inventoryPawn.inventory.innerContainer.TryAdd(inventoryProbe, false), "inventory probe added");
                middle.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                regions = middle.regionGrid.AllRegions_NoRebuild_InvalidAllowed.ToArray();
                districts = middle.regionGrid.allDistricts.ToArray();
                Check(regions.Length > 0 && districts.Length > 0, "map has regions and districts");
                GameComponent_MapManager component = GameComponent_MapManager.Instance!;
                component.GameComponentUpdate();
                long idleAllocated = GC.GetAllocatedBytesForCurrentThread();
                for (int update = 0; update < 256; update++) component.GameComponentUpdate();
                idleAllocated = GC.GetAllocatedBytesForCurrentThread() - idleAllocated;
                Check(idleAllocated == 0, "idle unified manager allocates zero bytes across 256 updates");
                Current.Game.CurrentMap = tail;
                component.Hibernate(middle);
                Check(component.IsHibernateRequested(middle) && Current.Game.Maps.Contains(middle) && !component.IsHibernating(middle),
                    "hibernation requests always queue until component update");
                component.GameComponentUpdate();
                Check(!Current.Game.Maps.Contains(middle) && component.IsHibernating(middle), "hibernating map is owned outside the active list");
                Check(!middle.Disposed && probes.All(probe => !probe.Spawned && probe.Map == null && !probe.Destroyed), "hibernating things are unspawned without being destroyed");
                Check(regions.All(region => region.Map == null) && districts.All(district => district.Map == null), "hibernating regions and districts leave the active map index");
                Check(tailProbe.Map == tail && Current.Game.CurrentMap == tail, "other map native indices and selection stay unchanged");
                Check(middle.Parent.Map == null && !middle.Parent.HasMap, "native map parent treats the detached map as unavailable");
                int mapTicks = middle.GetComponent<HibernationMapProbe>().Ticks;
                int mapUpdates = middle.GetComponent<HibernationMapProbe>().Updates;
                RunTicks(2100);
                Check(probes.All(probe => probe.Ticks == 0), "normal rare long things stop through native scheduling");
                Check(inventoryProbe.Ticks == 0 && inventoryProbe.MapHeld == middle, "held inventory things stop with their scheduled pawn");
                Check(middle.GetComponent<HibernationMapProbe>().Ticks == mapTicks && middle.GetComponent<HibernationMapProbe>().Updates == mapUpdates,
                    "map tick and update stop");
                Check(tailProbe.Ticks == 2100, "active map continues exactly one normal tick per game tick");
                component.Activate(middle);
                Check(Current.Game.Maps.Last() == middle && probes.All(probe => probe.Map == middle), "activation appends the original map and restores native indices");
                RunTicks(2100);
                Check(probes[0].Ticks == 2100 && probes[1].Ticks >= 8 && probes[2].Ticks >= 1, "all ticker types resume");
                Check(inventoryProbe.Ticks == 2100, "inventory ticking resumes through its pawn without duplicate registration");
                Check(tailProbe.Ticks == 4200, "active maps are unaffected by activation");
                component.Hibernate(middle);
                component.GameComponentUpdate();
                Map retrieved = GetOrGenerateMapUtility.GetOrGenerateMap(middle.Tile, middle.Size, null);
                Check(ReferenceEquals(retrieved, middle) && !component.IsHibernating(middle), "GetOrGenerateMap activates existing map without generation");
                int beforeRepeat = probes[0].Ticks;
                component.Hibernate(middle); component.Hibernate(middle); component.GameComponentUpdate();
                component.Activate(middle); component.Activate(middle);
                RunTicks(10);
                Check(probes[0].Ticks == beforeRepeat + 10, "repeated transitions never duplicate tick registrations");
                component.Hibernate(middle);
                RunTicks(1);
                Check(component.IsHibernateRequested(middle) && Current.Game.Maps.Contains(middle), "DoSingleTick does not flush queued hibernation");
                component.Activate(middle); component.GameComponentUpdate();
                Check(!component.IsHibernating(middle) && !component.IsHibernateRequested(middle), "activation cancels pending hibernation");
                component.Hibernate(middle);
                retrieved = GetOrGenerateMapUtility.GetOrGenerateMap(middle.Tile, middle.Size, null);
                component.GameComponentUpdate();
                Check(ReferenceEquals(retrieved, middle) && !component.IsHibernating(middle) && !component.IsHibernateRequested(middle),
                    "retrieving an active map cancels its pending hibernation");
                component.Hibernate(middle);
                Current.Game.CurrentMap = middle;
                component.GameComponentUpdate();
                Check(!component.IsHibernating(middle) && !component.IsHibernateRequested(middle), "map selection cancels queued hibernation");
                component.Hibernate(middle);
                component.GameComponentUpdate();
                Current.Game.CurrentMap = middle;
                Check(!component.IsHibernating(middle) && Current.Game.CurrentMap == middle, "map selection activates before switching");
                component.Hibernate(middle);
                GenSpawn.Spawn(ThingMaker.MakeThing(probes[0].def), new IntVec3(16, 0, 10), middle);
                component.GameComponentUpdate();
                Check(!component.IsHibernating(middle) && !component.IsHibernateRequested(middle), "spawning cancels queued hibernation");
                component.Hibernate(middle);
                probes[0].DeSpawn();
                component.GameComponentUpdate();
                Check(!component.IsHibernating(middle) && !component.IsHibernateRequested(middle), "despawning cancels queued hibernation");
                GenSpawn.Spawn(probes[0], new IntVec3(10, 0, 10), middle);

                component.Hibernate(middle);
                component.GameComponentUpdate();
                component.Hibernate(tail);
                Scribe.mode = LoadSaveMode.Saving;
                try
                {
                    component.GameComponentUpdate();
                    Check(component.IsHibernateRequested(tail) && Current.Game.Maps.Contains(tail), "component update does not process requests during saving");
                }
                finally { Scribe.mode = LoadSaveMode.Inactive; }
                GameDataSaveLoader.SaveGame("CQF_PendingHibernationChecks");
                XDocument pendingSave = XDocument.Load(GenFilePaths.FilePathForSavedGame("CQF_PendingHibernationChecks"));
                Check(pendingSave.Descendants("CQF_pendingHibernation").Elements("li").Any(element => element.Value == tail.GetUniqueLoadID()),
                    "save records pending hibernation requests");
                Check(component.IsHibernateRequested(tail) && Current.Game.Maps.Contains(tail), "saving preserves pending runtime requests");
                component.Activate(tail);
                middleID = middle.uniqueID;
                tailID = tail.uniqueID;
                savedProbeTicks = probes.Select(probe => probe.Ticks).ToArray();
                savedThingIDs = probes.Select(probe => probe.ThingID).ToArray();
                savedMapTicks = middle.GetComponent<HibernationMapProbe>().Ticks;
                GameComponent_Editor.Instance.GlobalDatabase.RecordTarget("CQF_HibernatingMap", new TargetInfo(new IntVec3(10, 0, 10), middle));
                GameDataSaveLoader.SaveGame("CQF_HibernationChecks");
                string savePath = GenFilePaths.FilePathForSavedGame("CQF_HibernationChecks");
                Check(File.Exists(savePath), "save written");
                XDocument save = XDocument.Load(savePath);
                Check(save.Descendants("maps").First().Elements("li").Count() + save.Descendants("CQF_ownedHibernatingMaps").Elements("li").Count() == component.TotalMapCount,
                    "save includes active and hibernating maps");
                Check(!Current.Game.Maps.Contains(middle) && component.IsHibernating(middle), "save returns the sleeping map to component ownership");
                XElement gameNode = save.Root!.Element("game")!;
                XElement managerNode = gameNode.Descendants("li").Single(element =>
                    element.Attribute("Class")?.Value == "QuestEditor_Library.GameComponent_MapManager");
                XElement owned = managerNode.Element("CQF_ownedHibernatingMaps")!;
                XElement references = new("CQF_hibernatingMaps");
                foreach (XElement record in owned.Elements("li"))
                {
                    XElement mapData = record.Element("map")!;
                    references.Add(new XElement("li", "Map_" + mapData.Element("uniqueID")!.Value));
                    gameNode.Element("maps")!.Add(new XElement("li", mapData.Attributes(), mapData.Elements()));
                }
                owned.Remove();
                managerNode.Add(references);
                save.Save(GenFilePaths.FilePathForSavedGame("CQF_LegacyHibernationChecks"));
                Check(references.Elements("li").Count() == 1, "pre-3.6 reference-list save fixture written with full native maps");
                GameDataSaveLoader.LoadGame("CQF_LegacyHibernationChecks");
                stage = 1;
                return;
            }
            if (stage == 1)
            {
                GameComponent_MapManager component = GameComponent_MapManager.Instance!;
                Map loaded = component.HibernatingMaps.Single(map => map.uniqueID == middleID);
                Check(!Current.Game.Maps.Contains(loaded) && !loaded.Disposed, "load reconstructs the component-owned detached map");
                HibernationProbe[] loadedProbes = savedThingIDs.Select(id => loaded.listerThings.AllThings.OfType<HibernationProbe>().Single(probe => probe.ThingID == id)).ToArray();
                Check(loadedProbes.Select(probe => probe.Ticks).SequenceEqual(savedProbeTicks), "load preserves things and ticker state");
                Check(loadedProbes.All(probe => !probe.Spawned && probe.Map == null), "loaded hibernating things are unspawned");
                Check(loaded.GetComponent<HibernationMapProbe>().Ticks == savedMapTicks, "loaded hibernating map never ticks during load completion");
                Check(loaded.Parent is MapParent_Custom custom && custom.sourceMap != null && Current.Game.Maps.Contains(custom.sourceMap),
                    "load resolves pocket map source reference");
                TargetInfo recorded = GameComponent_Editor.Instance.GlobalDatabase.GetTarget("CQF_HibernatingMap");
                Check(recorded.Map == loaded, "load resolves CQF database map reference");
                component.Activate(loaded);
                Check(Current.Game.Maps.Count(map => map.uniqueID == middleID) == 1, "loaded map activation never regenerates");
                int beforeTick = loadedProbes[0].Ticks;
                RunTicks(10);
                Check(loadedProbes[0].Ticks == beforeTick + 10, "loaded map ticks resume without duplicates");
                component.Hibernate(loaded);
                component.GameComponentUpdate();
                component.Activate(loaded);
                PocketMapUtility.DestroyPocketMap(loaded);
                Check(loaded.Disposed && !component.IsHibernating(loaded) && !Current.Game.Maps.Contains(loaded), "explicit destruction cleans hibernating map");
                Check(Current.Game.Maps.Any(map => map.uniqueID == tailID), "destroying hibernating map preserves other maps");
                Map queuedRemoval = GenerateMap(Current.Game.Maps.First());
                component.Hibernate(queuedRemoval);
                PocketMapUtility.DestroyPocketMap(queuedRemoval);
                component.GameComponentUpdate();
                Check(queuedRemoval.Disposed && !component.IsHibernateRequested(queuedRemoval), "map removal cancels queued hibernation");
                foreach (Map map in Current.Game.Maps.ToArray()) component.Hibernate(map);
                component.GameComponentUpdate();
                Check(Current.Game.Maps.Count == 0 && Current.Game.CurrentMap == null && component.HibernatingMaps.Count == component.TotalMapCount, "all maps can hibernate");
                GameDataSaveLoader.SaveGame("CQF_AllMapsHibernating");
                Check(Current.Game.Maps.Count == 0 && Current.Game.CurrentMap == null && component.HibernatingMaps.Count == component.TotalMapCount, "saving all paused maps leaves the native map list unchanged");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_AllMapsHibernating")), "all hibernating maps save written");
                GameDataSaveLoader.LoadGame("CQF_AllMapsHibernating");
                stage = 2;
                return;
            }
            if (stage == 2)
            {
                GameComponent_MapManager component = GameComponent_MapManager.Instance!;
                Check(Current.Game.Maps.Count == 0 && Current.Game.CurrentMap == null && component.HibernatingMaps.Count == 2,
                    "load keeps both sleeping maps outside the active map list");
                Map loaded = component.HibernatingMaps.Single(map => map.uniqueID == tailID);
                Current.Game.CurrentMap = loaded;
                Check(Current.Game.Maps.Count == 1 && Current.Game.CurrentMap == loaded && !component.IsHibernating(loaded),
                    "map selection recovers from all maps hibernating");
                Complete(true);
            }
        }
        catch (Exception error)
        {
            if (root.Length == 0) root = GenFilePaths.SaveDataFolderPath;
            File.AppendAllText(Path.Combine(root, "HibernationChecks.txt"), "FAIL stage " + stage + "\n" + error + "\n", Encoding.UTF8);
            Log.Error("CQF hibernation checks failed: " + error);
            Complete(false);
        }
    }

    private static Map GenerateMap(Map source)
    {
        MapParent_Custom parent = (MapParent_Custom)WorldObjectMaker.MakeWorldObject(QEDefOf.QE_CustomMap_SubMap);
        parent.Tile = new PlanetTile(0, source.Tile.Layer);
        while (Current.Game.Maps.Any(map => map.Tile == parent.Tile))
            parent.Tile = new PlanetTile(parent.Tile.tileId + 1, source.Tile.Layer);
        parent.permanent = true;
        parent.SetFaction(Faction.OfPlayer);
        return GameTools.GenerateSubMap(new IntVec3(40, 1, 40), parent, parent.def.mapGenerator, Enumerable.Empty<GenStepWithParams>(), source);
    }

    private static void RunTicks(int count)
    {
        for (int i = 0; i < count; i++) Find.TickManager.DoSingleTick();
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(root, "HibernationChecks.txt"), "PASS " + name + "\n", Encoding.UTF8);
    }

    private void Complete(bool success)
    {
        finished = true;
        File.AppendAllText(Path.Combine(root, "HibernationChecks.txt"), success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        Application.Quit(success ? 0 : 1);
    }

    private string root = string.Empty;
    private int stage;
    private bool finished;
    private Map home = null!;
    private Map middle = null!;
    private Map tail = null!;
    private HibernationProbe[] probes = Array.Empty<HibernationProbe>();
    private HibernationProbe tailProbe = null!;
    private Region[] regions = Array.Empty<Region>();
    private District[] districts = Array.Empty<District>();
    private int middleID;
    private int tailID;
    private int savedMapTicks;
    private int[] savedProbeTicks = Array.Empty<int>();
    private string[] savedThingIDs = Array.Empty<string>();
}
