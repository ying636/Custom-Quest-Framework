using Verse;

namespace QuestEditor_Library;

public sealed class DomainLayout : IExposable
{
    public DomainLayout() { }

    public DomainLayout(MapComponent_Domain domain, DomainMapTemplate template, bool adoptExisting)
    {
        this.domain = domain;
        foreach (DomainStructureBlueprint blueprint in template.Structures)
        {
            IntVec3 cell = template.PositionFor(blueprint.Position, domain.map.Size);
            Building? building = adoptExisting ? cell.GetThingList(domain.map).OfType<Building>()
                .FirstOrDefault(candidate => candidate.Position == cell && candidate.def == blueprint.Definition && candidate.Rotation == blueprint.Rotation) : null;
            building ??= blueprint.Create();
            structures.Add(new DomainStructure(building, cell, blueprint.Rotation));
        }
    }

    public IReadOnlyList<DomainStructure> Structures => structures;

    public void Bind(MapComponent_Domain owner) => domain = owner;

    public DomainStructure Register(Building building, bool parkOnTerritoryLoss = true)
    {
        if (domain?.Ready != true || building.Destroyed || !building.Spawned || building.Map != domain.map
            || !building.OccupiedRect().All(domain.OwnsCell))
            throw new InvalidOperationException("[CQF] Domain structures must be spawned inside their active owner's territory.");
        if (domain.Battlefield.Layout?.Structures.Any(item => item.Thing == building) == true
            || domain.Battlefield.Challenger?.Layout?.Structures.Any(item => item.Thing == building) == true)
            throw new InvalidOperationException("[CQF] Domain structure is already registered.");
        DomainStructure structure = new(building, building.Position, building.Rotation, parkOnTerritoryLoss);
        structures.Add(structure);
        return structure;
    }

    public void ParkAll()
    {
        foreach (DomainStructure structure in structures) structure.Park();
    }

    public void ReleaseOccupants()
    {
        foreach (DomainStructure structure in structures) structure.ReleaseOccupants(domain!.map);
    }

    public void ParkOutside()
    {
        foreach (DomainStructure structure in structures)
            if (structure.ParkOnTerritoryLoss && structure.Thing is { Destroyed: false } && !Owned(structure)) structure.Park();
    }

    public void RevealOwned()
    {
        foreach (DomainStructure structure in structures)
            if (structure.Thing is { Destroyed: false } && Owned(structure)) structure.Reveal(domain!.map);
    }

    public void MoveTo(MapComponent_Domain destination, IntVec3 originalSize)
    {
        foreach (DomainStructure structure in structures) structure.MoveTo(destination.map, originalSize);
        domain = destination;
    }

    public void Clear()
    {
        foreach (DomainStructure structure in structures) structure.Clear();
        structures.Clear();
    }

    public void ExposeData() => Scribe_Collections.Look(ref structures, "structures", LookMode.Deep);

    private bool Owned(DomainStructure structure)
        => domain?.Active == true && structure.Footprint.All(cell => cell.InBounds(domain.map) && domain.OwnsCell(cell));

    private MapComponent_Domain? domain;
    private List<DomainStructure> structures = new();
}
