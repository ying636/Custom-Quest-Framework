using Verse;

namespace QuestEditor_Library;

public class Hediff_DomainProtection : Hediff
{
    public MapComponent_Domain? Domain => domainMap?.GetComponent<MapComponent_Domain>();

    public override bool Visible => false;

    public override bool ShouldRemove => Domain?.Active != true || Domain.Caster != pawn;

    public void Initialize(MapComponent_Domain domain)
    {
        domainMap = domain.map;
        EnsureProtection();
    }

    public void EnsureProtection()
    {
        protection = Comp_DomainProtection.Ensure(pawn);
        protection.Protection = this;
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
    {
        Domain?.Worker?.CasterDied(pawn);
    }

    public override void PostRemoved()
    {
        if (protection?.Protection == this) protection.Protection = null;
        protection = null;
        base.PostRemoved();
    }

    public override void Notify_Spawned()
    {
        base.Notify_Spawned();
        if (Domain?.Active == true) EnsureProtection();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref domainMap, "domainMap");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && Domain?.Active == true) EnsureProtection();
    }

    private Map? domainMap;
    private Comp_DomainProtection? protection;
}
