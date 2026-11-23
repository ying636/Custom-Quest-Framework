using Verse;

namespace QuestEditor_Library;

public enum CQFCompositeMapPlacement
{
    Horizontal,
    Vertical
}

public sealed class CQFCompositeMapGroup : IExposable
{
    public CQFCompositeMapGroup() { }

    internal CQFCompositeMapGroup(string entry, CQFCompositeMapPlacement placement,
        CustomMapEntrance_Composite? owner = null, string? parentRegion = null)
    {
        entryRegion = entry;
        this.placement = placement;
        this.owner = owner;
        this.parentRegion = parentRegion;
    }

    public string EntryRegion => entryRegion;
    public string? ParentRegion => parentRegion;
    public CustomMapEntrance_Composite? Owner => owner;
    public CQFCompositeMapPlacement Placement => placement;
    internal IReadOnlyList<CQFCompositeMapRegion> Regions => regions;
    internal IReadOnlyList<CQFCompositeMapConnection> Portals => portals;
    internal IReadOnlyList<CQFCompositeMapConnection> Edges =>
        definition is { } def ? def.edges : Array.Empty<CQFCompositeMapConnection>();
    internal int LayoutVersion { get; private set; }

    public void ExposeData()
    {
        Scribe_Values.Look(ref entryRegion!, "entryRegion", "");
        Scribe_Values.Look(ref parentRegion, "parentRegion");
        Scribe_Values.Look(ref placement, "placement");
        Scribe_References.Look(ref owner, "owner");
        Scribe_Collections.Look(ref appended, "appended", LookMode.Deep);
        appended ??= new();
    }

    internal void AddRegion(string id, string? from, IntVec2? position)
    {
        IntVec2 cell;
        if (position is { } specified)
        {
            if (regions.Exists(region => region.position == specified))
                throw new InvalidOperationException("[CQF] Composite map position is already used: " + specified);
            cell = specified;
        }
        else
        {
            CQFCompositeMapRegion previous = regions.First(region => region.id == from);
            bool vertical = placement == CQFCompositeMapPlacement.Vertical;
            cell = vertical ? new IntVec2(previous.position.x, previous.position.z - 1)
                : new IntVec2(previous.position.x + 1, previous.position.z);
            while (regions.Exists(region => region.position == cell))
            {
                if (vertical) cell.x++;
                else cell.z--;
            }
        }
        appended.Add(new CQFCompositeMapRegion { id = id, position = cell, sourceRegion = from });
        Rebuild(definition);
    }

    internal void RemoveRegion(string id)
    {
        appended.RemoveAll(region => region.id == id);
        Rebuild(definition);
    }

    internal void Rebuild(CQFCompositeMapDef? fixedDefinition)
    {
        definition = fixedDefinition;
        regions.Clear();
        portals.Clear();
        if (definition != null)
        {
            regions.AddRange(definition.regions);
            portals.AddRange(definition.portals);
        }
        regions.AddRange(appended);
        foreach (CQFCompositeMapRegion region in appended)
            if (region.sourceRegion is string from && regions.Exists(candidate => candidate.id == from))
                portals.Add(new CQFCompositeMapConnection { from = from, to = region.id });
        LayoutVersion++;
    }

    private string entryRegion = "";
    private string? parentRegion;
    private CQFCompositeMapPlacement placement;
    private CustomMapEntrance_Composite? owner;
    private List<CQFCompositeMapRegion> appended = new();
    private CQFCompositeMapDef? definition;
    private readonly List<CQFCompositeMapRegion> regions = new();
    private readonly List<CQFCompositeMapConnection> portals = new();
}
