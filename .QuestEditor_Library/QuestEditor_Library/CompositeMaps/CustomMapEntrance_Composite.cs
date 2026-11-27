using RimWorld;
using Verse;

namespace QuestEditor_Library;

public class CustomMapEntrance_Composite : CustomMapEntrance_Chance
{
    public CQFCompositeMapPlacement Placement => def.GetModExtension<ModExtension_CQFCompositeEntrance>()?.layout
        ?? CQFCompositeMapPlacement.Vertical;
    public CQFCompositeMapGroup? CompositeGroup => GameComponent_MapManager.Instance?.GetCompositeGroup(this);
    internal Map? FixedSource => fixedSource;
    internal bool FixedOneWay => fixedSource != null && fixedArrival.IsValid;
    public override CQFMapPortal CooldownDestination => FixedOneWay ? this : base.CooldownDestination;
    public override bool DestroyMapWhenDestroy => fixedSource == null && base.DestroyMapWhenDestroy;
    public override bool DestroyMapWhenDeSpawn =>
        fixedSource == null && GameComponent_MapManager.Instance?.CompositeBranchRemovalBlock(this) == null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref fixedSource, "fixedCompositeSource");
        Scribe_Values.Look(ref fixedArrival, "fixedCompositeArrival", IntVec3.Invalid);
    }

    public override bool IsEnterable(out string reason)
    {
        if (fixedSource != null && (CustomMap is null or { Disposed: true } ||
            (!FixedOneWay && exit is null or { Destroyed: true })))
        {
            reason = "EntranceIsBlocked".Translate();
            return false;
        }
        return base.IsEnterable(out reason);
    }

    public override IntVec3 GetDestinationLocation() => FixedOneWay ? fixedArrival : base.GetDestinationLocation();

    public override void SetMapDef(CustomMapDataDef mapDef)
    {
        if (tagWithChance.Count == 0 && mapDefWithChance.Count == 0)
            this.mapDef = mapDef;
        else
            base.SetMapDef(mapDef);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        GameComponent_MapManager? manager = GameComponent_MapManager.Instance;
        if (fixedSource != null)
        {
            base.Destroy(mode);
            manager?.InvalidateCrossMapRoutes();
            return;
        }
        if (manager?.CompositeBranchRemovalBlock(this) is { } blocked)
        {
            Messages.Message(blocked.Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        manager?.DestroyEntranceCompositeBranch(this);
        base.Destroy(mode);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!preserveMapOnDespawn && DestroyMapWhenDeSpawn)
            GameComponent_MapManager.Instance?.DestroyEntranceCompositeBranch(this);
        base.DeSpawn(mode);
    }

    internal void BindFixedPassage(Map source, Map destination, CustomMapExit? pairedExit, IntVec3 arrival)
    {
        fixedSource = source;
        fixedArrival = pairedExit == null ? arrival : IntVec3.Invalid;
        CustomMap = destination;
        exit = pairedExit;
        if (pairedExit != null) pairedExit.entrance = this;
    }

    protected internal override void NotifySubMapGenerated(Map source, Map destination) =>
        GameComponent_MapManager.Instance?.RegisterCompositeEntrance(this, source, destination);

    private Map? fixedSource;
    private IntVec3 fixedArrival = IntVec3.Invalid;
}
