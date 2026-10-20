using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class DomainOpening : Thing
{
    public abstract DomainDef Definition { get; }
    public abstract Pawn? Caster { get; }
    public abstract bool Active { get; }
    public virtual bool SupportsFusion => false;
    public virtual float Progress => 1f;
    public virtual float Radius => Definition.visuals?.radius ?? 12f;
    public virtual IReadOnlyList<Pawn> Entrants => Array.Empty<Pawn>();
    public DomainOpening_Fusion? Fusion { get => fusion; internal set => fusion = value; }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_Domain>().RegisterOpening(this);
        runtime = CQFDomainRuntime.Instance;
        runtime.RegisterOpening(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetComponent<MapComponent_Domain>().UnregisterOpening(this);
        runtime?.UnregisterOpening(this);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref fusion, "fusion");
    }

    public virtual void ReleaseForFusion() { }
    public virtual void ResumeAfterFusion() { }

    public abstract void CompleteOpening();
    public abstract void CompleteTransfer();

    private DomainOpening_Fusion? fusion;
    private CQFDomainRuntime? runtime;
}
