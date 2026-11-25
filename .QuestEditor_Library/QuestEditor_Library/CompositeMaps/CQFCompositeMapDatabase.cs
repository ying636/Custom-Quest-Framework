using Verse;

namespace QuestEditor_Library;

public enum CQFCompositeMapLookup
{
    RegionId,
    MapKey,
    MapTemplate
}

public sealed class CQFCompositeMapDatabase : IExposable
{
    public Map? Find(string key, CQFCompositeMapLookup lookup, CQFCompositeMapGroup? group = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();
        if (lookup == CQFCompositeMapLookup.RegionId)
            return regions.TryGetValue(key, out var entry) && Accepts(entry.Map, entry.Group, group) ? entry.Map : null;
        if (lookup == CQFCompositeMapLookup.MapKey && bindings.TryGetValue(key, out string? region))
            return Find(region, CQFCompositeMapLookup.RegionId, group);
        if (!indexes.TryGetValue((lookup, key), out var matches)) return null;
        Map? result = null;
        foreach (var match in matches)
        {
            if (!Accepts(match.Map, match.Group, group)) continue;
            if (result != null) return null;
            result = match.Map;
        }
        return result;
    }

    public bool Bind(string key, Map map)
    {
        if (string.IsNullOrWhiteSpace(key) || map.Disposed || !regionsByMap.TryGetValue(map, out string? id))
            return false;
        bindings[key.Trim()] = id;
        return true;
    }

    public bool Unbind(string key) => !string.IsNullOrWhiteSpace(key) && bindings.Remove(key.Trim());

    public void ExposeData()
    {
        Scribe_Collections.Look(ref bindings, "bindings", LookMode.Value, LookMode.Value);
        bindings ??= new(StringComparer.Ordinal);
    }

    internal void Register(CQFCompositeMapRegion region, Map map, CQFCompositeMapGroup group)
    {
        regions.Add(region.id, (map, group));
        regionsByMap.Add(map, region.id);
        CustomMapDataDef? template = region.map ?? (map.Parent as MapParent_Custom)?.mapDataDef;
        Add(CQFCompositeMapLookup.MapTemplate, template?.Origin.defName);
        Add(CQFCompositeMapLookup.MapKey, region.mapKey.NullOrEmpty() ? template?.compositeMapKey : region.mapKey);

        void Add(CQFCompositeMapLookup lookup, string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            var index = (lookup, key.Trim());
            if (!indexes.TryGetValue(index, out var entries)) indexes.Add(index, entries = new());
            entries.Add((map, group));
        }
    }

    internal void ClearIndex()
    {
        regions.Clear();
        regionsByMap.Clear();
        indexes.Clear();
    }

    internal void PruneBindings()
    {
        if (bindings.Count == 0) return;
        foreach (string key in bindings.Where(binding => !regions.ContainsKey(binding.Value)).Select(binding => binding.Key).ToArray())
            bindings.Remove(key);
    }

    private static bool Accepts(Map map, CQFCompositeMapGroup group, CQFCompositeMapGroup? requested) =>
        !map.Disposed && (requested == null || group == requested);

    private Dictionary<string, string> bindings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Map Map, CQFCompositeMapGroup Group)> regions = new(StringComparer.Ordinal);
    private readonly Dictionary<Map, string> regionsByMap = new();
    private readonly Dictionary<(CQFCompositeMapLookup Lookup, string Key), List<(Map Map, CQFCompositeMapGroup Group)>> indexes = new();
}
