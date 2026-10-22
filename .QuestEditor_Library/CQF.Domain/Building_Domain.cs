using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class Building_Domain : Building
{
    public MapComponent_Domain? Domain => domainMap?.GetComponent<MapComponent_Domain>().FindDomainFor(domainCaster);
    public DomainOpening_Fusion? OpeningFusion { get => openingFusion; internal set => openingFusion = value; }

    public virtual void Initialize(Map realm)
    {
        domainMap = realm;
        domainCaster = realm.GetComponent<MapComponent_Domain>().Caster;
        SynchronizeHitPoints();
    }

    public void BindDomain(MapComponent_Domain domain)
    {
        domainMap = domain.map;
        domainCaster = domain.Caster;
        SynchronizeHitPoints();
    }

    public DomainFusionState DrawFusion()
    {
        if (openingFusion?.Active == true) return openingFusion.DomeStateFor(this);
        MapComponent_Domain? domain = Domain;
        float blend = domain?.Contest?.DrawSource(domain, this) ?? 0f;
        return blend > 0f ? domain!.Contest!.DomeStateFor(domain, blend) : default;
    }

    public void SynchronizeHitPoints()
    {
        if (!Destroyed && Domain != null) HitPoints = Mathf.Max(1, Mathf.CeilToInt(Domain.Health));
    }

    public void RemoveNormally()
    {
        domainMap = null;
        if (!Destroyed) Destroy();
    }

    public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        base.PreApplyDamage(ref dinfo, out absorbed);
        if (absorbed) return;
        MapComponent_Domain? domain = Domain;
        if (domain?.Active != true)
        {
            Log.Error("[CQF] Domain anchor received damage without an active domain: " + ThingID);
            return;
        }
        absorbed = true;
        if (dinfo.Def.harmsHealth && dinfo.Amount > 0f)
            domain.DrainHealth(Mathf.Min(dinfo.Amount, domain.Definition.maximumAnchorDamagePerHit));
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        MapComponent_Domain? domain = Domain;
        domainMap = null;
        base.Destroy(mode);
        domain?.Collapse("domain anchor destroyed: " + mode);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        MapComponent_Domain? domain = Domain;
        domainMap = null;
        base.DeSpawn(mode);
        domain?.Collapse("domain anchor removed: " + mode);
    }

    public override string GetInspectString()
    {
        string original = base.GetInspectString();
        MapComponent_Domain? domain = Domain;
        if (domain == null) return original;
        string shared = domain.Health.ToString("N0") + " / " + domain.MaximumHealth.ToString("N0");
        if (domain.Caster != null) shared = domain.Caster.LabelShortCap + "\n" + shared;
        return original.NullOrEmpty() ? shared : original + "\n" + shared;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref domainMap, "domainMap");
        Scribe_References.Look(ref domainCaster, "domainCaster", saveDestroyedThings: true);
        Scribe_References.Look(ref openingFusion, "openingFusion");
    }

    private Map? domainMap;
    private Pawn? domainCaster;
    private DomainOpening_Fusion? openingFusion;
}
