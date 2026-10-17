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
    }

    private void ExposeHibernationData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            savedMaps = records.Values.ToList();
            savedRequests = pendingHibernation.ToList();
        }
        Scribe_Collections.Look(ref savedMaps, "CQF_ownedHibernatingMaps", LookMode.Deep);
        Scribe_Collections.Look(ref savedRequests, "CQF_pendingHibernation", LookMode.Reference);
    }

    private void RestoreHibernatingMaps()
    {
        if (savedMaps != null)
        {
            foreach (CQFHibernatingMap record in savedMaps)
            {
                Map map = record.Map;
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
        if (savedRequests == null) return;
        foreach (Map map in savedRequests) Hibernate(map);
        savedRequests.Clear();
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
    }

    private readonly List<Map> hibernatingMaps = new();
    private readonly Dictionary<Map, CQFHibernatingMap> records = new();
    private readonly HashSet<Map> pendingHibernation = new();
    private List<CQFHibernatingMap>? savedMaps;
    private List<Map>? savedRequests;
}
