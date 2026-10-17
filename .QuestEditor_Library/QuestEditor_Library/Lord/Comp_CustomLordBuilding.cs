using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed class Comp_CustomLordBuilding : ThingComp
{
    public static void Ensure(Building building)
    {
        List<ThingComp>? comps = (List<ThingComp>?)CompsField.GetValue(building);
        if (comps == null)
        {
            comps = new List<ThingComp>();
            CompsField.SetValue(building, comps);
        }
        if (comps.Any(comp => comp is Comp_CustomLordBuilding)) return;
        Comp_CustomLordBuilding protection = new Comp_CustomLordBuilding { parent = building };
        protection.Initialize(new CompProperties(typeof(Comp_CustomLordBuilding)));
        comps.Add(protection);
    }

    public override bool CompPreventClaimingBy(Faction faction)
    {
        return parent is Building building && building.GetLord()?.LordJob is LordJob_Custom;
    }

    private static readonly FieldInfo CompsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ThingWithComps).FullName, "comps");
}
