using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapRegionCollection : IExposable
{
    public IReadOnlyList<CQFMapRegion> All => regions;

    public string NextScope(string prefix)
    {
        string value;
        do { value = prefix + "/" + ++sequence; } while (regions.Any(r => r.scope == value || r.id == value));
        return value;
    }

    public void Add(CQFMapRegion region, string scope = "", Map? map = null)
    {
        region.Validate(map);
        string id = string.IsNullOrEmpty(scope) ? region.key : scope + "/" + region.key;
        if (regions.Any(r => r.id == id)) throw new CQFLayoutException("CQF_Region_Duplicate: " + id);
        region.id = id; region.scope = scope;
        regions.Add(region);
    }

    public void Remove(CQFMapRegion region) => regions.Remove(region);

    public void Rename(CQFMapRegion region, string key)
    {
        string id = string.IsNullOrEmpty(region.scope) ? key : region.scope + "/" + key;
        if (string.IsNullOrWhiteSpace(key) || regions.Any(r => r != region && r.id == id))
            throw new CQFLayoutException("CQF_Region_Duplicate: " + id);
        region.key = key; region.id = id;
    }

    public List<CQFMapRegion> Select(string key, IEnumerable<string> tags, string scope = "", bool requireSingle = true)
    {
        List<string> required = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        IEnumerable<CQFMapRegion> query = regions;
        if (!string.IsNullOrEmpty(key))
        {
            string local = string.IsNullOrEmpty(scope) ? key : scope + "/" + key;
            List<CQFMapRegion> exact = regions.Where(r => r.id == local).ToList();
            query = exact.Count > 0 ? exact : regions.Where(r => r.id == key || r.key == key);
        }
        else if (!string.IsNullOrEmpty(scope)) query = query.Where(r => r.scope == scope);
        List<CQFMapRegion> result = query.Where(r => required.All(r.tags.Contains)).OrderBy(r => r.id, StringComparer.Ordinal).ToList();
        if (result.Count == 0 || requireSingle && result.Count != 1)
            throw new CQFLayoutException("CQF_Region_Reference: " + key + " [" + string.Join(",", required) + "] = " + result.Count);
        return result;
    }

    public List<CQFMapRegion> Capture(IEnumerable<IntVec3> selected, IntVec3 origin)
    {
        HashSet<IntVec3> selection = new HashSet<IntVec3>(selected);
        List<CQFMapRegion> result = new List<CQFMapRegion>();
        foreach (CQFMapRegion region in regions)
        {
            if (!region.cells.Any(selection.Contains)) continue;
            region.Validate();
            if (!region.cells.All(selection.Contains)) throw new CQFLayoutException("CQF_Region_PartialExport: " + region.id);
            CQFMapRegion copy = region.Copy(c => c - origin);
            copy.key = string.IsNullOrEmpty(region.id) ? region.key : region.id;
            copy.id = ""; copy.scope = "";
            result.Add(copy);
        }
        return result;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref regions, "regions", LookMode.Deep);
        Scribe_Values.Look(ref sequence, "sequence");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            regions ??= new List<CQFMapRegion>();
            if (regions.Any(r => r == null) || regions.GroupBy(r => r.id).Any(g => g.Count() > 1))
                throw new CQFLayoutException("CQF_Region_InvalidRegistry");
        }
    }

    private List<CQFMapRegion> regions = new List<CQFMapRegion>();
    private int sequence;
}
