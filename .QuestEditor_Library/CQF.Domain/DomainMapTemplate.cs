using Verse;

namespace QuestEditor_Library;

public sealed class DomainMapTemplate : IExposable
{
    public DomainMapTemplate() { }

    public DomainMapTemplate(Map source)
    {
        size = source.Size;
        terrain = new DomainTerrainSnapshot(source);
        DomainWorker worker = source.GetComponent<MapComponent_Domain>().Worker
            ?? throw new InvalidOperationException("[CQF] Domain map template requires a configured worker.");
        foreach (Building building in source.listerThings.AllThings.OfType<Building>())
            if (building is not Building_Domain) structures.Add(worker.CaptureStructure(building));
    }

    public DomainTerrainSnapshot Terrain => terrain ?? throw new InvalidOperationException("[CQF] Domain map template has no terrain.");
    public IReadOnlyList<DomainStructureBlueprint> Structures => structures;

    public IntVec3 PositionFor(IntVec3 position, IntVec3 destinationSize)
        => position + new IntVec3(destinationSize.x / 2 - size.x / 2, 0, destinationSize.z / 2 - size.z / 2);

    public void ExposeData()
    {
        Scribe_Values.Look(ref size, "size");
        Scribe_Deep.Look(ref terrain, "terrain");
        Scribe_Collections.Look(ref structures, "structures", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (terrain == null || size.x <= 0 || size.z <= 0 || structures == null))
            throw new InvalidOperationException("[CQF] Invalid saved domain map template.");
    }

    private IntVec3 size;
    private DomainTerrainSnapshot? terrain;
    private List<DomainStructureBlueprint> structures = new();
}
