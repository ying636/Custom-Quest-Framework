using System.Reflection;
using RimWorld;
using Verse;

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

    public bool IsInvulnerable()
    {
        Pawn pawn = (Pawn)parent;
        if (pawn.Dead || pawn.Downed) return false;
        if (pawn.MapHeld?.GetComponent<MapComponent_Domain>()?.ProtectsFromOpening(pawn) == true) return true;
        AbilityDef? ability = pawn.CurJob?.ability?.def;
        if (ability?.comps != null)
            foreach (AbilityCompProperties properties in ability.comps)
                if (properties is AbilityCompProperties_DomainProtection) return true;
        return false;
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        if (IsInvulnerable())
        {
            dinfo.SetAmount(0f);
            absorbed = true;
            return;
        }
        absorbed = Protection?.Domain?.AbsorbDamage((Pawn)parent, ref dinfo) == true;
    }

    private static readonly FieldInfo CompsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ThingWithComps).FullName, "comps");
}
