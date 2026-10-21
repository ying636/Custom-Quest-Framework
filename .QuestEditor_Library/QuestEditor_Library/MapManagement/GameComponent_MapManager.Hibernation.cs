using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public IReadOnlyList<Map> HibernatingMaps => hibernatingMaps;
    public bool HasHibernatingMaps => records.Count > 0;
    public int TotalMapCount => game.Maps.Count + hibernatingMaps.Count;

    public bool IsHibernating(Map? map) => map != null && records.ContainsKey(map);

    public bool IsHibernateRequested(Map? map) => map != null && pendingHibernation.Contains(map);

    public void Hibernate(Map map)
    {
        ValidateMap(map);
        if (!IsHibernating(map)) pendingHibernation.Add(map);
    }

    public Map Activate(Map map)
    {
        ValidateMap(map);
        pendingHibernation.Remove(map);
        if (!records.TryGetValue(map, out CQFHibernatingMap? record)) return map;
        if (game.Maps.Count > sbyte.MaxValue)
            throw new InvalidOperationException("[CQF] Cannot activate a map: the vanilla map index limit has been reached.");
        game.Maps.Add(map);
        try
        {
            CQFHibernatingMap.SetMapIndex(map, (sbyte)map.Index);
            map.regionAndRoomUpdater.Enabled = record.RegionUpdaterEnabled;
            foreach (Thing thing in map.spawnedThings) Find.TickManager.RegisterAllTickabilityFor(thing);
        }
        catch (Exception error)
        {
            if (map.spawnedThings.Count > 0) Find.TickManager.RemoveAllFromMap(map);
            CQFHibernatingMap.SetMapIndex(map, -1);
            game.Maps.Remove(map);
            map.regionAndRoomUpdater.Enabled = false;
            throw new InvalidOperationException("[CQF] Map activation failed: " + map.GetUniqueLoadID(), error);
        }
        records.Remove(map);
        hibernatingMaps.Remove(map);
        AttachPocketMapParent(map);
        if (Current.ProgramState == ProgramState.Playing) Find.ColonistBar.MarkColonistsDirty();
        Module?.MapAdded(map);
        return map;
    }

    public void NotifyMapActivity(Map map) => pendingHibernation.Remove(map);

    public void ForgetMap(Map map)
    {
        pendingHibernation.Remove(map);
        records.Remove(map);
        hibernatingMaps.Remove(map);
        // A restored map is dropped without ever being activated, so its parent never made it back into
        // World.pocketMaps - and a map that was removed while active would leave its parent behind there.
        DetachPocketMapParent(map);
    }

    private void ExposeHibernationData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            savedMaps = records.Values.ToList();
            savedRequests = pendingHibernation.ToList();
        }
        Scribe_Collections.Look(ref savedMaps, "CQF_ownedHibernatingMaps", LookMode.Deep);
        MigrateLegacyHibernatingMaps();
        Scribe_Collections.Look(ref savedRequests, "CQF_pendingHibernation", LookMode.Reference);
    }

    // Saves made before the ownership rewrite keep their hibernating maps under the shorter
    // "CQF_hibernatingMaps" label, and nothing reads that node anymore, so upgrading silently lost
    // every detached map and left unresolvable map references in the domain layer. Reading the old
    // node is free when it is absent: Scribe_Collections.Look leaves the target untouched unless the
    // node exists, and nothing is written back under the old label, so each save migrates once.
    private void MigrateLegacyHibernatingMaps()
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars || savedMaps != null) return;
        List<CQFHibernatingMap>? legacyMaps = null;
        Scribe_Collections.Look(ref legacyMaps, "CQF_hibernatingMaps", LookMode.Deep);
        if (legacyMaps == null || legacyMaps.Count == 0) return;
        int skipped = 0;
        foreach (CQFHibernatingMap record in legacyMaps)
        {
            if (record?.MapOrDefault == null) { skipped++; continue; }
            (savedMaps ??= new List<CQFHibernatingMap>()).Add(record);
        }
        if (savedMaps != null)
            Log.Message("[CQF] Recovered " + savedMaps.Count + " hibernating map(s) from the legacy CQF_hibernatingMaps entry.");
        if (skipped > 0)
            Log.Warning("[CQF] Skipped " + skipped + " legacy CQF_hibernatingMaps entr" + (skipped == 1 ? "y" : "ies")
                + " without an embedded map. Saves from before 3.6 only stored a map reference there, so those maps stay awake instead of hibernating.");
    }

    private void RestoreHibernatingMaps()
    {
        Dictionary<int, Map> registered = new();
        foreach (Map map in game.Maps) registered.Add(map.uniqueID, map);
        if (savedMaps != null)
            foreach (CQFHibernatingMap record in savedMaps)
            {
                if (record == null) throw new InvalidOperationException("[CQF] CQF_ownedHibernatingMaps contains an empty map record.");
                Map map = record.Map;
                if (registered.ContainsKey(map.uniqueID))
                    throw new InvalidOperationException("[CQF] Duplicate saved map identity: " + map.GetUniqueLoadID());
                registered.Add(map.uniqueID, map);
            }
        if (savedRequests != null)
            foreach (Map map in savedRequests)
                if (map == null || !registered.TryGetValue(map.uniqueID, out Map? saved) || !ReferenceEquals(saved, map))
                    throw new InvalidOperationException("[CQF] A saved hibernation request has an unresolved or unregistered map.");
        if (savedMaps != null)
        {
            foreach (CQFHibernatingMap record in savedMaps)
            {
                Map map = record.Map;
                if (map.info != null && map.info.parent == null && record.Parent != null) map.info.parent = record.Parent;
                if (game.Maps.Contains(map) || records.ContainsKey(map))
                    throw new InvalidOperationException("[CQF] Duplicate saved hibernating map: " + map.GetUniqueLoadID());
                if (game.Maps.Count > sbyte.MaxValue)
                    throw new InvalidOperationException("[CQF] Cannot initialize a hibernating map: the vanilla map index limit has been reached.");
                records.Add(map, record);
                hibernatingMaps.Add(map);
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (Current.Game != game || map.Disposed) return;
                    if (game.Maps.Count > sbyte.MaxValue)
                        throw new InvalidOperationException("[CQF] Cannot initialize a hibernating map: the vanilla map index limit has been reached.");
                    game.Maps.Add(map);
                    try
                    {
                        map.FinalizeLoading();
                        map.Parent?.FinalizeLoading();
                    }
                    finally
                    {
                        if (map.spawnedThings.Count > 0) Find.TickManager.RemoveAllFromMap(map);
                        LongEventHandler.ExecuteWhenFinished(() => DetachMap(map));
                    }
                });
            }
            savedMaps.Clear();
        }
        if (savedRequests != null)
        {
            foreach (Map map in savedRequests)
                if (!IsHibernating(map)) Hibernate(map);
            savedRequests.Clear();
        }
        if (pendingHibernation.Count > 0) FlushRequests();
    }

    private void DisposeHibernatingMaps()
    {
        Map[] maps = hibernatingMaps.ToArray();
        hibernatingMaps.Clear();
        records.Clear();
        pendingHibernation.Clear();
        savedMaps?.Clear();
        savedRequests?.Clear();
        foreach (Map map in maps) map.Dispose();
    }

    private void ValidateMap(Map map)
    {
        if (map == null) throw new ArgumentNullException(nameof(map), "[CQF] A map is required for hibernation or activation.");
        if (Scribe.mode != LoadSaveMode.Inactive)
            throw new InvalidOperationException("[CQF] Cannot change map hibernation during serialization.");
        if (map.Disposed || (!game.Maps.Contains(map) && !records.ContainsKey(map)))
            throw new InvalidOperationException("[CQF] Map is not managed by this game: " + map.GetUniqueLoadID());
    }

    private void FlushRequests()
    {
        foreach (Map map in pendingHibernation.ToArray())
        {
            pendingHibernation.Remove(map);
            ValidateMap(map);
            HibernateNow(map, new CQFHibernatingMap(map));
        }
    }

    private void HibernateNow(Map map, CQFHibernatingMap record)
    {
        Map? selected = game.CurrentMap;
        if (Current.ProgramState == ProgramState.Playing)
        {
            map.weatherManager.EndAllSustainers();
            Find.SoundRoot.sustainerManager.EndAllInMap(map);
        }
        if (selected == map)
        {
            selected = game.Maps.FirstOrDefault(other => other != map && !IsHibernateRequested(other));
            game.CurrentMap = selected;
            if (selected == null && Current.ProgramState == ProgramState.Playing)
                game.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
        }
        if (map.spawnedThings.Count > 0) Find.TickManager.RemoveAllFromMap(map);
        DetachMap(map);
        records.Add(map, record);
        hibernatingMaps.Add(map);
        if (Current.ProgramState == ProgramState.Playing) Find.ColonistBar.MarkColonistsDirty();
    }

    private void DetachMap(Map map)
    {
        Map? selected = game.CurrentMap;
        int index = game.Maps.IndexOf(map);
        if (index < 0) throw new InvalidOperationException("[CQF] Cannot detach an unregistered map: " + map.GetUniqueLoadID());
        CQFHibernatingMap.SetMapIndex(map, -1);
        game.Maps.RemoveAt(index);
        for (int i = index; i < game.Maps.Count; i++) CQFHibernatingMap.SetMapIndex(game.Maps[i], (sbyte)i);
        game.currentMapIndex = selected == null || selected == map ? (sbyte)-1 : (sbyte)game.Maps.IndexOf(selected);
        map.regionAndRoomUpdater.Enabled = false;
        DetachPocketMapParent(map);
    }

    // Hibernation removes a map from Game.maps without destroying it, which vanilla never does: every
    // consumer of World.pocketMaps assumes a listed PocketMapParent still owns a live map and
    // dereferences PocketMapParent.Map without a null check (Game.DeinitAndRemoveMap, Map.ChildPocketMaps,
    // MapParent.ShouldRemoveMapNow, ...). Keeping the parent out of that list while the map is detached
    // is what makes hibernation safe for vanilla code; the parent then travels inside the hibernation
    // record instead (see CQFHibernatingMap.ExposeData) and comes back here when the map is activated.
    private void AttachPocketMapParent(Map map)
    {
        if (map.Parent is not PocketMapParent parent) return;
        List<PocketMapParent>? pocketMaps = game.World?.pocketMaps;
        if (pocketMaps == null || pocketMaps.Contains(parent)) return;
        pocketMaps.Add(parent);
    }

    private void DetachPocketMapParent(Map map)
    {
        if (map.Parent is not PocketMapParent parent) return;
        game.World?.pocketMaps?.Remove(parent);
    }

    // Saves written before the invariant above existed still list detached parents in World.pocketMaps.
    // Hibernating maps are not re-registered yet when this runs (RestoreHibernatingMaps defers that to
    // the end of the load), so those parents have no live map either - they are skipped here and dropped
    // by DetachMap once the map is re-registered, while anything else without a map can only make
    // vanilla throw and is removed.
    private void CleanPocketMapParents()
    {
        List<PocketMapParent>? pocketMaps = game.World?.pocketMaps;
        if (pocketMaps == null) return;
        int removed = pocketMaps.RemoveAll(parent => parent == null
            || (parent.Map == null && !IsHibernationParent(parent)));
        if (removed > 0)
            Log.Warning("[CQF] Dropped " + removed + " pocket map entr" + (removed == 1 ? "y" : "ies")
                + " whose map no longer exists.");
    }

    private bool IsHibernationParent(PocketMapParent parent)
    {
        foreach (CQFHibernatingMap record in records.Values)
        {
            Map? map = record.MapOrDefault;
            if (map != null && ReferenceEquals(map.info?.parent, parent)) return true;
        }
        return false;
    }

    private readonly List<Map> hibernatingMaps = new();
    private readonly Dictionary<Map, CQFHibernatingMap> records = new();
    private readonly HashSet<Map> pendingHibernation = new();
    private List<CQFHibernatingMap>? savedMaps;
    private List<Map>? savedRequests;
}
