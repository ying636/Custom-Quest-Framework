using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapRegion : IExposable, ISaveable
{
    public string Label => string.IsNullOrWhiteSpace(name) ? key : name;
    public CellRect Bounds => cells.Count == 0 ? CellRect.Empty : CellRect.FromLimits(cells.Min(c => c.x), cells.Min(c => c.z), cells.Max(c => c.x), cells.Max(c => c.z));
    public List<IntVec3> Boundary
    {
        get
        {
            HashSet<IntVec3> set = new HashSet<IntVec3>(cells);
            return cells.Where(c => GenAdj.CardinalDirections.Any(d => !set.Contains(c + d))).ToList();
        }
    }

    public CQFMapRegion Copy(Func<IntVec3, IntVec3>? transform = null)
    {
        CQFMapRegion result = new CQFMapRegion
        {
            key = key, name = name, id = id, scope = scope, protect = protect,
            tags = new List<string>(tags), cells = cells.Select(c => transform == null ? c : transform(c)).Distinct().ToList(),
            connections = connections.Select(c => c.Copy()).ToList()
        };
        if (transform != null) result.connections.ForEach(c => c.cell = transform(c.cell));
        return result;
    }

    public double DistanceTo(IntVec3 cell)
    {
        if (cells.Count == 0) throw new CQFLayoutException("CQF_Region_Empty: " + key);
        return Math.Sqrt(cells.Min(c => (double)(c.x - cell.x) * (c.x - cell.x) + (double)(c.z - cell.z) * (c.z - cell.z)));
    }

    public void Validate(Map? map = null)
    {
        if (string.IsNullOrWhiteSpace(key) || cells.Count == 0 || cells.Any(c => map != null && !c.InBounds(map)))
            throw new CQFLayoutException("CQF_Region_Invalid: " + key);
        if (connections.Any(c => c.width < 1 || !c.direction.IsValid || !cells.Contains(c.cell)
            || cells.Contains(c.cell + c.direction.FacingCell)))
            throw new CQFLayoutException("CQF_Region_InvalidConnection: " + key);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key", "");
        Scribe_Values.Look(ref name, "name", "");
        Scribe_Values.Look(ref id, "id", "");
        Scribe_Values.Look(ref scope, "scope", "");
        Scribe_Values.Look(ref protect, "protect");
        Scribe_Collections.Look(ref tags, "tags", LookMode.Value);
        Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
        Scribe_Collections.Look(ref connections, "connections", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tags ??= new List<string>(); cells ??= new List<IntVec3>(); connections ??= new List<CQFMapRegionConnection>();
        }
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName,
        new XElement("key", key), new XElement("name", name), new XElement("protect", protect),
        CQFSerialization.SaveList(tags, "tags"), CQFSerialization.SaveList(cells, "cells"),
        CQFSerialization.SaveList_Saveable(connections, "connections"));

    public string key = "";
    public string name = "";
    public List<string> tags = new List<string>();
    public List<IntVec3> cells = new List<IntVec3>();
    public List<CQFMapRegionConnection> connections = new List<CQFMapRegionConnection>();
    public bool protect;
    public string id = "";
    public string scope = "";
}
