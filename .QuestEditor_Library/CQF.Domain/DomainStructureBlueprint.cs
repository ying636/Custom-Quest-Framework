using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainStructureBlueprint : IExposable
{
    public DomainStructureBlueprint() { }

    public DomainStructureBlueprint(Building building)
    {
        definition = building.def;
        stuff = building.Stuff;
        position = building.Position;
        rotation = building.Rotation;
        hitPoints = building.HitPoints;
        faction = building.Faction;
        quality = building.GetComp<CompQuality>()?.Quality;
        CompColorable? colorable = building.GetComp<CompColorable>();
        if (colorable?.Active == true) color = colorable.Color;
        style = building.GetComp<CompStyleable>()?.styleDef;
    }

    public ThingDef Definition => definition ?? throw new InvalidOperationException("[CQF] Structure blueprint has no definition.");
    public IntVec3 Position => position;
    public Rot4 Rotation => rotation;

    public virtual Building Create()
    {
        Building building = ThingMaker.MakeThing(Definition, stuff) as Building
            ?? throw new InvalidOperationException("[CQF] Domain structure blueprint must create a building.");
        if (Definition.CanHaveFaction) building.SetFaction(faction);
        if (quality.HasValue) building.GetComp<CompQuality>()?.SetQuality(quality.Value, ArtGenerationContext.Outsider);
        if (color.HasValue) building.GetComp<CompColorable>()?.SetColor(color.Value);
        if (building.GetComp<CompStyleable>() is { } styleable) styleable.styleDef = style;
        if (Definition.useHitPoints) building.HitPoints = Math.Max(1, Math.Min(hitPoints, building.MaxHitPoints));
        return building;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref definition, "definition");
        Scribe_Defs.Look(ref stuff, "stuff");
        Scribe_Defs.Look(ref style, "style");
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref rotation, "rotation");
        Scribe_Values.Look(ref hitPoints, "hitPoints");
        Scribe_Values.Look(ref quality, "quality");
        Scribe_Values.Look(ref color, "color");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (definition == null || definition.category != ThingCategory.Building || !position.IsValid))
            throw new InvalidOperationException("[CQF] Invalid domain structure blueprint.");
    }

    private ThingDef? definition;
    private ThingDef? stuff;
    private ThingStyleDef? style;
    private Faction? faction;
    private IntVec3 position;
    private Rot4 rotation;
    private int hitPoints;
    private QualityCategory? quality;
    private Color? color;
}
