using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public class Comp_DomainProtection : ThingComp
{
    public Hediff_DomainProtection? Protection { get; set; }

    public static Comp_DomainProtection Ensure(Pawn pawn)
    {
        List<ThingComp>? comps = (List<ThingComp>?)CompsField.GetValue(pawn);
        if (comps == null)
        {
            comps = new List<ThingComp>();
            CompsField.SetValue(pawn, comps);
        }
        foreach (ThingComp existing in comps)
            if (existing is Comp_DomainProtection protection) return protection;
        Comp_DomainProtection component = new() { parent = pawn };
        component.Initialize(new CompProperties(typeof(Comp_DomainProtection)));
        comps.Insert(0, component);
        return component;
    }

    public bool HasOpeningProtection()
    {
        Pawn pawn = (Pawn)parent;
        if (pawn.Dead || pawn.Downed) return false;
        return TryGetOpeningShield(pawn, out _, out _, out _, out _, out _);
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        if (dinfo.Amount <= 0f)
        {
            absorbed = false;
            return;
        }
        Pawn pawn = (Pawn)parent;
        if (!pawn.Dead && !pawn.Downed && TryGetOpeningShield(pawn, out float maximum, out Job? job, out DomainOpening? opening,
            out MapComponent_Domain? transitionDomain, out AbilityDef? ability))
        {
            PrepareOpeningShield(maximum, job, opening, transitionDomain, ability);
            float amount = dinfo.Amount;
            float blocked = Mathf.Min(openingShieldRemaining, amount);
            openingShieldRemaining -= blocked;
            dinfo.SetAmount(amount - blocked);
            absorbed = dinfo.Amount <= 0f;
            return;
        }
        MapComponent_Domain? domain = Protection?.Domain ?? pawn.MapHeld?.GetComponent<MapComponent_Domain>()?.FindDomainFor(pawn);
        absorbed = domain?.AbsorbDamage(pawn, ref dinfo) == true;
    }

    public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
    {
        base.Notify_Killed(prevMap, dinfo);
        prevMap?.GetComponent<MapComponent_Domain>().CancelOpeningsFor((Pawn)parent);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref openingShieldRemaining, "openingShieldRemaining");
        Scribe_Values.Look(ref openingShieldInitialized, "openingShieldInitialized");
        Scribe_Defs.Look(ref openingShieldAbility, "openingShieldAbility");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            openingShieldJob = null;
            openingShieldOpening = null;
            openingShieldTransitionDomain = null;
            openingShieldNeedsSessionBinding = openingShieldInitialized;
        }
    }

    private bool TryGetOpeningShield(Pawn pawn, out float maximum, out Job? job, out DomainOpening? opening,
        out MapComponent_Domain? transitionDomain, out AbilityDef? ability)
    {
        maximum = 0f;
        job = null;
        opening = null;
        transitionDomain = null;
        ability = null;
        MapComponent_Domain? mapDomain = pawn.MapHeld?.GetComponent<MapComponent_Domain>();
        if (mapDomain?.TryGetOpeningContext(pawn, out DomainDef definition, out opening, out transitionDomain) == true)
        {
            ability = definition.openingAbility;
            maximum = ShieldFor(ability);
            return true;
        }
        job = pawn.CurJob;
        ability = job?.ability?.def;
        if (ability?.comps == null) return false;
        foreach (AbilityCompProperties properties in ability.comps)
        {
            if (properties is not AbilityCompProperties_DomainProtection protection) continue;
            maximum = protection.openingShield;
            return true;
        }
        job = null;
        ability = null;
        return false;
    }

    private static float ShieldFor(AbilityDef? ability)
    {
        if (ability?.comps == null) return 1000f;
        foreach (AbilityCompProperties properties in ability.comps)
            if (properties is AbilityCompProperties_DomainProtection protection) return protection.openingShield;
        return 1000f;
    }

    private void PrepareOpeningShield(float maximum, Job? job, DomainOpening? opening,
        MapComponent_Domain? transitionDomain, AbilityDef? ability)
    {
        if (openingShieldNeedsSessionBinding && openingShieldAbility == ability)
        {
            openingShieldJob = job;
            openingShieldOpening = opening;
            openingShieldTransitionDomain = transitionDomain;
            openingShieldNeedsSessionBinding = false;
            return;
        }
        openingShieldNeedsSessionBinding = false;
        if (openingShieldInitialized && IsSameOpeningShieldSession(job, opening, transitionDomain, ability)) return;
        bool continuing = openingShieldInitialized && (
            opening != null && openingShieldJob != null && ability == openingShieldAbility
                && opening.Definition.openingAbility == ability
            || transitionDomain != null && openingShieldOpening != null
                && transitionDomain.Definition.openingAbility == openingShieldAbility
            || transitionDomain != null && openingShieldJob != null
                && transitionDomain.Definition.openingAbility == openingShieldAbility);
        if (!continuing) openingShieldRemaining = maximum;
        openingShieldInitialized = true;
        openingShieldJob = job;
        openingShieldOpening = opening;
        openingShieldTransitionDomain = transitionDomain;
        openingShieldAbility = ability;
    }

    private bool IsSameOpeningShieldSession(Job? job, DomainOpening? opening,
        MapComponent_Domain? transitionDomain, AbilityDef? ability)
        => (job != null && openingShieldJob == job && openingShieldAbility == ability)
            || (opening != null && openingShieldOpening == opening && openingShieldAbility == ability)
            || (transitionDomain != null && openingShieldTransitionDomain == transitionDomain && openingShieldAbility == ability);

    private static readonly FieldInfo CompsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ThingWithComps).FullName, "comps");
    private float openingShieldRemaining;
    private Job? openingShieldJob;
    private DomainOpening? openingShieldOpening;
    private MapComponent_Domain? openingShieldTransitionDomain;
    private AbilityDef? openingShieldAbility;
    private bool openingShieldInitialized;
    private bool openingShieldNeedsSessionBinding;
}
