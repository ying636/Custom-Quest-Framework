using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEcologyPlan
{
    public Dictionary<IntVec3, TerrainDef> Terrains { get; } = new Dictionary<IntVec3, TerrainDef>();
    public Dictionary<IntVec3, ThingDef> Objects { get; } = new Dictionary<IntVec3, ThingDef>();
    public HashSet<IntVec3> Mutable { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> Water { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> Rocks { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> Trees { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> Obstacles { get; } = new HashSet<IntVec3>();
    public BiomeDef? Biome { get; set; }
    public string SourceSignature { get; set; } = "";

    public bool CanWalk(IntVec3 cell) => Terrains.TryGetValue(cell, out TerrainDef? terrain) && terrain.passability != Traversability.Impassable && !Rocks.Contains(cell) && !Obstacles.Contains(cell)
        && (!Objects.TryGetValue(cell, out ThingDef? thing) || thing.passability != Traversability.Impassable);

    public bool Matches(CQFEcologyPlan other) => Biome == other.Biome && SourceSignature == other.SourceSignature && Mutable.SetEquals(other.Mutable)
        && Terrains.Count == other.Terrains.Count && Terrains.All(p => other.Terrains.TryGetValue(p.Key, out TerrainDef? value) && p.Value == value)
        && Objects.Count == other.Objects.Count && Objects.All(p => other.Objects.TryGetValue(p.Key, out ThingDef? value) && p.Value == value);

    public void Apply(Map map)
    {
        foreach (IntVec3 cell in Mutable.OrderBy(c => c.z).ThenBy(c => c.x))
        {
            foreach (Thing thing in cell.GetThingList(map).ToList())
                if (thing is Plant || thing is Filth || thing.def.building?.isNaturalRock == true) thing.Destroy();
            map.terrainGrid.SetTerrain(cell, Terrains[cell]);
        }
        foreach (KeyValuePair<IntVec3, ThingDef> entry in Objects.OrderBy(p => p.Key.z).ThenBy(p => p.Key.x))
        {
            Thing thing = ThingMaker.MakeThing(entry.Value);
            if (thing is Plant plant) plant.Growth = 0.8f;
            GenSpawn.Spawn(thing, entry.Key, map);
        }
        if (Biome != null) map.GetComponent<MapComponent_CQFEcology>().Biome = Biome;
    }
}
