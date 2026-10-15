using System.Reflection;
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
        List<ThingComp>? comps = (List<ThingComp>?)CompsField.GetValue(pawn);
        if (comps == null)
        {
            comps = new List<ThingComp>();
            CompsField.SetValue(pawn, comps);
        }
        if (protection != null && comps.Contains(protection)) return;
        protection = new Comp_DomainProtection(this) { parent = pawn };
        protection.Initialize(new CompProperties(typeof(Comp_DomainProtection)));
        comps.Insert(0, protection);
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
    {
        Domain?.Worker?.CasterDied(pawn);
    }

    public override void PostRemoved()
    {
        if (protection != null) pawn.AllComps.Remove(protection);
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

    private static readonly FieldInfo CompsField = typeof(ThingWithComps).GetField("comps",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ThingWithComps).FullName, "comps");
    private Map? domainMap;
    private Comp_DomainProtection? protection;
}
