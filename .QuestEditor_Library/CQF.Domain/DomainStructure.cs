using Verse;
using RimWorld;

namespace QuestEditor_Library;

public sealed class DomainStructure : IExposable, IThingHolder
{
    public DomainStructure() { held = new ThingOwner<Thing>(this); }

    public DomainStructure(Building building, IntVec3 position, Rot4 rotation) : this(building, position, rotation, true) { }

    public DomainStructure(Building building, IntVec3 position, Rot4 rotation, bool parkOnTerritoryLoss) : this()
    {
        this.position = position;
        this.rotation = rotation;
        this.parkOnTerritoryLoss = parkOnTerritoryLoss;
        if (building.Spawned) spawned = building;
        else if (!held.TryAdd(building, false)) throw new InvalidOperationException("[CQF] Could not hold a domain structure.");
    }

    public IThingHolder? ParentHolder => null;
    public Thing? Thing => spawned ?? held.FirstOrDefault();
    public IntVec3 Position => position;
    public CellRect Footprint => GenAdj.OccupiedRect(position, rotation, Thing!.def.size);
    public bool ParkOnTerritoryLoss => parkOnTerritoryLoss;

    public void Park()
    {
        if (spawned == null || spawned.Destroyed || !spawned.Spawned) return;
        Thing building = spawned;
        ReleaseOccupants(building.Map);
        building.DeSpawn();
        if (!held.TryAdd(building, false)) throw new InvalidOperationException("[CQF] Could not park a domain structure.");
        spawned = null;
    }

    public void ReleaseOccupants(Map map)
    {
        Thing? building = Thing;
        if (building == null || building.Destroyed) return;
        if (building is Building_Casket { Spawned: true, HasAnyContents: true } casket) casket.EjectContents();
        List<IThingHolder> holders = new();
        if (building is IThingHolder root) holders.Add(root);
        if (building is ThingWithComps withComps)
            foreach (ThingComp comp in withComps.AllComps)
                if (comp is IThingHolder child) holders.Add(child);
        HashSet<IThingHolder> visited = new();
        for (int index = 0; index < holders.Count; index++)
        {
            IThingHolder holder = holders[index];
            if (holder is Pawn || !visited.Add(holder)) continue;
            ThingOwner contents = holder.GetDirectlyHeldThings();
            foreach (Thing occupant in contents.ToList())
                if (occupant is Pawn or Corpse && !contents.TryDrop(occupant, position, map, ThingPlaceMode.Near, out _))
                    throw new InvalidOperationException("[CQF] Could not release a domain structure occupant: " + occupant.ThingID);
            holder.GetChildHolders(holders);
        }
    }

    public bool Reveal(Map map)
    {
        if (spawned?.Destroyed == true || spawned?.Spawned == true || held.Count == 0) return false;
        if (Footprint.Any(cell => !cell.InBounds(map) || cell.GetThingList(map).Any(thing => thing is not Filth))) return false;
        Thing building = held[0];
        held.Remove(building);
        try { spawned = GenSpawn.Spawn(building, position, map, rotation); }
        catch
        {
            if (!building.Spawned && !building.Destroyed && !held.TryAdd(building, false))
                Log.Error("[CQF] Could not recover a domain structure after spawning failed: " + building.ThingID);
            throw;
        }
        return true;
    }

    public void MoveTo(Map destination, IntVec3 originalSize)
    {
        if (spawned?.Spawned == true) position = spawned.Position;
        else position = new IntVec3(Math.Min(destination.Size.x - 1, position.x * destination.Size.x / originalSize.x), 0,
            Math.Min(destination.Size.z - 1, position.z * destination.Size.z / originalSize.z));
    }

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        foreach (Thing thing in held)
        {
            if (thing is IThingHolder holder) outChildren.Add(holder);
            if (thing is ThingWithComps withComps)
                foreach (ThingComp comp in withComps.AllComps)
                    if (comp is IThingHolder child) outChildren.Add(child);
        }
    }

    public void Clear()
    {
        if (spawned != null && !spawned.Destroyed) spawned.Destroy();
        spawned = null;
        foreach (Thing thing in held.ToList()) thing.Destroy();
        held.Clear();
    }

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving && spawned?.Destroyed == true) spawned = null;
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref rotation, "rotation");
        Scribe_Values.Look(ref parkOnTerritoryLoss, "parkOnTerritoryLoss", true);
        Scribe_References.Look(ref spawned, "spawned", saveDestroyedThings: true);
        Scribe_Deep.Look(ref held, "held", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (held == null || held.Count > 1 || held.Count > 0 && spawned != null))
            throw new InvalidOperationException("[CQF] Invalid saved domain structure ownership.");
    }

    private Thing? spawned;
    private ThingOwner<Thing> held;
    private IntVec3 position;
    private Rot4 rotation;
    private bool parkOnTerritoryLoss = true;
}
