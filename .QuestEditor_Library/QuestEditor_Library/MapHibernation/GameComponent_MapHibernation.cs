using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    public sealed class GameComponent_MapHibernation : GameComponent
    {
        public GameComponent_MapHibernation(Game game)
        {
            this.game = game;
            instance = this;
        }

        public static GameComponent_MapHibernation? Instance =>
            instance != null && ReferenceEquals(instance.game, Current.Game) ? instance : null;
        public IReadOnlyList<Map> HibernatingMaps => hibernatingMaps.AsReadOnly();
        public bool HasHibernatingMaps => records.Count > 0;
        public int TotalMapCount => game.Maps.Count + (saving ? 0 : hibernatingMaps.Count);
        public bool HasHibernatingColonists => hibernatingMaps.Any(map =>
            map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount > 0 ||
            map.mapPawns.AllPawnsSpawned.Any(pawn => pawn.carryTracker?.CarriedThing is Pawn carried && carried.IsFreeColonist));

        public bool IsHibernating(Map map)
        {
            return map != null && records.ContainsKey(map);
        }

        public bool IsHibernateRequested(Map map)
        {
            return map != null && pendingHibernation.Contains(map);
        }

        public void Hibernate(Map map)
        {
            ValidateMap(map);
            if (IsHibernating(map) || pendingHibernation.Contains(map))
            {
                return;
            }
            if (executionDepth > 0)
            {
                pendingHibernation.Add(map);
                return;
            }
            HibernateNow(map);
        }

        public Map Activate(Map map)
        {
            ValidateMap(map);
            pendingHibernation.Remove(map);
            if (!records.TryGetValue(map, out CQFHibernatingMap record))
            {
                return map;
            }
            List<Map> originalMaps = new List<Map>(game.Maps);
            sbyte originalCurrentIndex = game.currentMapIndex;
            try
            {
                game.AddMap(map);
                if (!game.Maps.Contains(map))
                {
                    throw new InvalidOperationException("[CQF] Failed to register hibernating map " + map.GetUniqueLoadID());
                }
                ReindexMap(map, (sbyte)game.Maps.IndexOf(map));
                RemoveReferences(record);
                records.Remove(map);
                hibernatingMaps.Remove(map);
                map.regionAndRoomUpdater.Enabled = record.RegionUpdaterEnabled;
                Find.TickManager.RemoveAllFromMap(map);
                foreach (Thing thing in record.Things)
                {
                    Find.TickManager.RegisterAllTickabilityFor(thing);
                }
                Find.ColonistBar.MarkColonistsDirty();
                return map;
            }
            catch (Exception error)
            {
                Find.TickManager.RemoveAllFromMap(map);
                game.Maps.Clear();
                game.Maps.AddRange(originalMaps);
                game.currentMapIndex = originalCurrentIndex;
                records[map] = record;
                if (!hibernatingMaps.Contains(map))
                {
                    hibernatingMaps.Add(map);
                }
                AddReferences(record);
                map.regionAndRoomUpdater.Enabled = false;
                throw new InvalidOperationException("[CQF] Map activation failed: " + map.GetUniqueLoadID(), error);
            }
        }

        public bool TryGetMap(Thing thing, out Map map)
        {
            return thingMaps.TryGetValue(thing, out map);
        }

        public bool TryGetMap(Region region, out Map map)
        {
            return regionMaps.TryGetValue(region, out map);
        }

        public bool TryGetMap(District district, out Map map)
        {
            return districtMaps.TryGetValue(district, out map);
        }

        public bool TryGetMap(MapParent parent, out Map map)
        {
            return parentMaps.TryGetValue(parent, out map);
        }

        public Map? FindHibernatingMap(PlanetTile tile)
        {
            return hibernatingMaps.FirstOrDefault(map => map.Tile == tile);
        }

        public void BeginExecution()
        {
            executionDepth++;
        }

        public void EndExecution(bool completed)
        {
            if (executionDepth <= 0)
            {
                throw new InvalidOperationException("[CQF] Unbalanced map hibernation execution scope.");
            }
            executionDepth--;
            if (completed && executionDepth == 0 && !saving)
            {
                FlushRequests();
            }
        }

        public void BeginSave()
        {
            if (saving)
            {
                throw new InvalidOperationException("[CQF] Nested map hibernation save scope.");
            }
            if (TotalMapCount > sbyte.MaxValue + 1)
            {
                throw new InvalidOperationException("[CQF] Too many maps to serialize with vanilla map indices.");
            }
            Map[] appended = hibernatingMaps.ToArray();
            saveCurrentMapIndex = game.currentMapIndex;
            saving = true;
            game.Maps.AddRange(appended);
            if (game.currentMapIndex < 0 && game.Maps.Count > 0)
            {
                game.currentMapIndex = 0;
            }
            for (int i = 0; i < appended.Length; i++)
            {
                ReindexMap(appended[i], (sbyte)game.Maps.IndexOf(appended[i]));
            }
        }

        public void EndSave()
        {
            if (!saving)
            {
                return;
            }
            foreach (Map map in hibernatingMaps)
            {
                game.Maps.Remove(map);
            }
            game.currentMapIndex = saveCurrentMapIndex;
            saving = false;
        }

        public void CheckCanAddMap(Map map)
        {
            if (saving)
            {
                throw new InvalidOperationException("[CQF] Cannot register maps during serialization.");
            }
            if (map != null && !game.Maps.Contains(map) && !IsHibernating(map) && TotalMapCount >= sbyte.MaxValue + 1)
            {
                throw new InvalidOperationException("[CQF] Active and hibernating maps reached the vanilla map index limit.");
            }
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedHibernatingMaps = hibernatingMaps.Concat(pendingHibernation).Distinct().ToList();
            }
            Scribe_Collections.Look(ref savedHibernatingMaps, "CQF_hibernatingMaps", LookMode.Reference);
        }

        public override void LoadedGame()
        {
            if (savedHibernatingMaps == null)
            {
                return;
            }
            foreach (Map map in savedHibernatingMaps)
            {
                ValidateMap(map);
                HibernateNow(map);
            }
            savedHibernatingMaps.Clear();
        }

        public void DisposeHibernatingMaps()
        {
            foreach (Map map in hibernatingMaps)
            {
                map.Dispose();
            }
            hibernatingMaps.Clear();
            records.Clear();
            thingMaps.Clear();
            regionMaps.Clear();
            districtMaps.Clear();
            parentMaps.Clear();
            pendingHibernation.Clear();
            savedHibernatingMaps?.Clear();
            if (ReferenceEquals(instance, this))
            {
                instance = null;
            }
        }

        private void ValidateMap(Map map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map), "[CQF] A map is required for hibernation or activation.");
            }
            if (saving)
            {
                throw new InvalidOperationException("[CQF] Cannot change map hibernation during serialization.");
            }
            if (map.Disposed || (!game.Maps.Contains(map) && !IsHibernating(map)))
            {
                throw new InvalidOperationException("[CQF] Map is disposed or does not belong to this game: " + map.GetUniqueLoadID());
            }
        }

        private void FlushRequests()
        {
            foreach (Map map in pendingHibernation.ToArray())
            {
                pendingHibernation.Remove(map);
                ValidateMap(map);
                HibernateNow(map);
            }
        }

        private void HibernateNow(Map map)
        {
            map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
            CQFHibernatingMap record = new CQFHibernatingMap(map);
            List<Map> originalMaps = new List<Map>(game.Maps);
            sbyte originalCurrentIndex = game.currentMapIndex;
            Map selected = game.CurrentMap;
            try
            {
                AddReferences(record);
                records.Add(map, record);
                hibernatingMaps.Add(map);
                Find.TickManager.RemoveAllFromMap(map);
                map.weatherManager.EndAllSustainers();
                Find.SoundRoot.sustainerManager.EndAllInMap(map);
                if (selected == map)
                {
                    selected = game.Maps.FirstOrDefault(other => other != map);
                    game.CurrentMap = selected;
                    if (selected == null)
                    {
                        game.World.renderer.wantedMode = WorldRenderMode.Planet;
                    }
                }
                game.Maps.Remove(map);
                for (int i = 0; i < game.Maps.Count; i++)
                {
                    ReindexMap(game.Maps[i], (sbyte)i);
                }
                game.currentMapIndex = (sbyte)(selected == null ? -1 : game.Maps.IndexOf(selected));
                map.regionAndRoomUpdater.Enabled = false;
                Find.ColonistBar.MarkColonistsDirty();
            }
            catch (Exception error)
            {
                game.Maps.Clear();
                game.Maps.AddRange(originalMaps);
                RemoveReferences(record);
                records.Remove(map);
                hibernatingMaps.Remove(map);
                for (int i = 0; i < game.Maps.Count; i++)
                {
                    ReindexMap(game.Maps[i], (sbyte)i);
                }
                game.currentMapIndex = originalCurrentIndex;
                map.regionAndRoomUpdater.Enabled = record.RegionUpdaterEnabled;
                Find.TickManager.RemoveAllFromMap(map);
                foreach (Thing thing in record.Things)
                {
                    Find.TickManager.RegisterAllTickabilityFor(thing);
                }
                throw new InvalidOperationException("[CQF] Map hibernation failed: " + map.GetUniqueLoadID(), error);
            }
        }

        private void AddReferences(CQFHibernatingMap record)
        {
            foreach (Thing thing in record.Things)
            {
                thingMaps[thing] = record.Map;
            }
            foreach (Region region in record.Regions)
            {
                regionMaps[region] = record.Map;
            }
            foreach (District district in record.Districts)
            {
                districtMaps[district] = record.Map;
            }
            if (record.Map.Parent != null)
            {
                parentMaps[record.Map.Parent] = record.Map;
            }
        }

        private void RemoveReferences(CQFHibernatingMap record)
        {
            foreach (Thing thing in record.Things)
            {
                thingMaps.Remove(thing);
            }
            foreach (Region region in record.Regions)
            {
                regionMaps.Remove(region);
            }
            foreach (District district in record.Districts)
            {
                districtMaps.Remove(district);
            }
            if (record.Map.Parent != null)
            {
                parentMaps.Remove(record.Map.Parent);
            }
        }

        private static void ReindexMap(Map map, sbyte index)
        {
            foreach (Thing thing in map.spawnedThings)
            {
                thingMapIndex(thing) = index;
            }
            foreach (Region region in map.regionGrid.AllRegions_NoRebuild_InvalidAllowed)
            {
                region.mapIndex = index;
                if (region.District != null)
                {
                    region.District.mapIndex = index;
                }
            }
            foreach (District district in map.regionGrid.allDistricts)
            {
                district.mapIndex = index;
            }
            foreach (Room room in map.regionGrid.AllRooms)
            {
                foreach (District district in room.Districts)
                {
                    district.mapIndex = index;
                }
            }
        }

        private readonly Game game;
        private readonly List<Map> hibernatingMaps = new List<Map>();
        private readonly Dictionary<Map, CQFHibernatingMap> records = new Dictionary<Map, CQFHibernatingMap>();
        private readonly Dictionary<Thing, Map> thingMaps = new Dictionary<Thing, Map>();
        private readonly Dictionary<Region, Map> regionMaps = new Dictionary<Region, Map>();
        private readonly Dictionary<District, Map> districtMaps = new Dictionary<District, Map>();
        private readonly Dictionary<MapParent, Map> parentMaps = new Dictionary<MapParent, Map>();
        private readonly HashSet<Map> pendingHibernation = new HashSet<Map>();
        private List<Map> savedHibernatingMaps = new List<Map>();
        private int executionDepth;
        private bool saving;
        private sbyte saveCurrentMapIndex;
        private static GameComponent_MapHibernation? instance;
        private static readonly AccessTools.FieldRef<Thing, sbyte> thingMapIndex =
            AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");
    }
}
