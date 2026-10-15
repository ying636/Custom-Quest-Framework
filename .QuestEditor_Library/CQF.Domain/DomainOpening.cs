using Verse;

namespace QuestEditor_Library;

public abstract class DomainOpening : Thing
{
    public abstract DomainDef Definition { get; }
    public abstract Pawn? Caster { get; }
    public abstract bool Active { get; }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_Domain>().RegisterOpening(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetComponent<MapComponent_Domain>().UnregisterOpening(this);
        base.DeSpawn(mode);
    }

    public abstract void CompleteOpening();
    public abstract void CompleteTransfer();
}
