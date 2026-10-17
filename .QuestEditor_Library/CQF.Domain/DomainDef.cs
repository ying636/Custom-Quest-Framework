using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public class DomainDef : Def
{
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        if (openingAbility == null) return;
        lock (openingAbility)
        {
            openingAbility.comps ??= new List<AbilityCompProperties>();
            if (!openingAbility.comps.Any(properties => properties is AbilityCompProperties_DomainProtection))
                openingAbility.comps.Add(new AbilityCompProperties_DomainProtection());
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (worldObject == null) yield return "Domain requires a world object definition.";
        else if (!typeof(MapParent_Domain).IsAssignableFrom(worldObject.worldObjectClass))
            yield return "Domain world object must derive from MapParent_Domain.";
        if (mapGenerator == null) yield return "Domain requires a map generator.";
        if (workerClass == null || !typeof(DomainWorker).IsAssignableFrom(workerClass) || workerClass.IsAbstract
            || workerClass.GetConstructor(Type.EmptyTypes) == null)
            yield return "Domain worker must be a concrete DomainWorker.";
        if (maximumHealth <= 0f || float.IsNaN(maximumHealth) || float.IsInfinity(maximumHealth))
            yield return "Domain maximum health must be finite and positive.";
        if (maximumAnchorDamagePerHit <= 0f || float.IsNaN(maximumAnchorDamagePerHit)) yield return "Domain anchor damage limit must be positive.";
        if (size.x <= 0 || size.z <= 0 || size.y != 1) yield return "Domain requires a positive map size with y=1.";
        if (anchor != null && !typeof(Building_Domain).IsAssignableFrom(anchor.thingClass))
            yield return "Domain anchor must derive from Building_Domain.";
        if (protection != null && !typeof(Hediff_DomainProtection).IsAssignableFrom(protection.hediffClass))
            yield return "Domain protection must derive from Hediff_DomainProtection.";
    }

    public WorldObjectDef worldObject = null!;
    public MapGeneratorDef mapGenerator = null!;
    public Type workerClass = typeof(DomainWorker);
    public IntVec3 size = new(60, 1, 60);
    public float maximumHealth = 1000f;
    public float maximumAnchorDamagePerHit = float.MaxValue;
    public ThingDef? anchor;
    public HediffDef? protection;
    public AbilityDef? openingAbility;
    public bool preload = true;
}
