using System.Security.Cryptography;
using System.Text;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEcologyGenerator
{
    public CQFEcologyPlan Preview(Map map, CQFEcologySettings settings, int seed, HashSet<IntVec3> protectedCells, HashSet<IntVec3> occupied)
    {
        Dictionary<IntVec3, TerrainDef> terrain = map.AllCells.ToDictionary(c => c, c => map.terrainGrid.TerrainAt(c));
        HashSet<IntVec3> rocks = new HashSet<IntVec3>();
        HashSet<IntVec3> trees = new HashSet<IntVec3>();
        HashSet<IntVec3> obstacles = new HashSet<IntVec3>();
        StringBuilder signature = new StringBuilder();
        foreach (IntVec3 cell in map.AllCells)
            signature.Append(terrain[cell].defName).Append('/').Append(map.roofGrid.RoofAt(cell)?.defName).Append(';');
        foreach (Thing thing in map.listerThings.AllThings.OrderBy(t => t.thingIDNumber))
        {
            signature.Append(thing.ThingID).Append(':').Append(thing.Position).Append(':')
                .Append(thing is Building_Door && thing.def.size.x == 1 && thing.def.size.z == 1 ? Rot4.North : thing.Rotation)
                .Append(':').Append(thing.HitPoints).Append(':').Append(thing.stackCount).Append(';');
            if (thing.def.building?.isNaturalRock == true) rocks.UnionWith(thing.OccupiedRect().Cells);
            else if (thing.def.passability == Traversability.Impassable) obstacles.UnionWith(thing.OccupiedRect().Cells);
            if (thing.def.plant?.IsTree == true) trees.UnionWith(thing.OccupiedRect().Cells);
        }
        IEnumerable<IntVec3> mutable = settings.regenerate
            ? map.AllCells.Where(c => !protectedCells.Contains(c) && !occupied.Contains(c) && !c.Roofed(map)) : Enumerable.Empty<IntVec3>();
        CQFEcologyPlan plan = Create(settings, settings.biome ?? map.Biome, seed, terrain, mutable, rocks, trees);
        plan.Obstacles.UnionWith(obstacles.Where(c => !plan.Mutable.Contains(c)));
        using SHA256 hash = SHA256.Create();
        plan.SourceSignature = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(signature.ToString())));
        return plan;
    }

    public CQFEcologyPlan Create(CQFEcologySettings settings, BiomeDef biome, int seed, IReadOnlyDictionary<IntVec3, TerrainDef> original,
        IEnumerable<IntVec3> mutable, IEnumerable<IntVec3>? rocks = null, IEnumerable<IntVec3>? trees = null)
    {
        settings.Validate();
        CQFEcologyPlan plan = new CQFEcologyPlan();
        foreach (var entry in original) plan.Terrains.Add(entry.Key, entry.Value);
        if (rocks != null) plan.Rocks.UnionWith(rocks);
        if (trees != null) plan.Trees.UnionWith(trees);
        if (!settings.regenerate)
        {
            plan.Water.UnionWith(original.Where(p => p.Value.IsWater).Select(p => p.Key)); return plan;
        }
        if (biome == null) throw new CQFLayoutException("CQF_Ecology_MissingBiome");
        plan.Biome = biome; plan.Mutable.UnionWith(mutable);
        plan.Rocks.ExceptWith(plan.Mutable); plan.Trees.ExceptWith(plan.Mutable);
        TerrainDef? water = settings.waterTerrain ?? biome.waterShallowTerrain;
        if (settings.waterCoverage > 0f && (water == null || !water.IsWater)) throw new CQFLayoutException("CQF_Ecology_MissingWater");
        ThingDef? rock = settings.rock ?? biome.forceRockTypes?.FirstOrDefault(d => d.building?.isNaturalRock == true)
            ?? DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.building?.isNaturalRock == true && d.building.isResourceRock == false && d.size.x == 1 && d.size.z == 1).OrderBy(d => d.defName, StringComparer.Ordinal).FirstOrDefault();
        if (settings.rockCoverage > 0f && rock == null) throw new CQFLayoutException("CQF_Ecology_MissingRock");
        List<IntVec3> cells = plan.Mutable.OrderBy(c => Noise(c, settings.patchSize, seed, 7)).ThenBy(c => c.z).ThenBy(c => c.x).ToList();
        HashSet<IntVec3> waterCells = new HashSet<IntVec3>(cells.Take((int)(cells.Count * settings.waterCoverage)));
        HashSet<IntVec3> rockCells = new HashSet<IntVec3>(cells.Where(c => !waterCells.Contains(c)).OrderByDescending(c => Noise(c, settings.patchSize, seed, 19))
            .ThenBy(c => c.z).ThenBy(c => c.x).Take((int)(cells.Count * settings.rockCoverage)));
        List<(ThingDef Plant, float Weight)> plants = settings.plants.Count > 0 ? settings.plants.Select(p => (p.plant!, p.weight)).ToList()
            : biome.wildPlants.Where(p => p.plant?.plant != null && p.commonality > 0f && p.plant.size.x == 1 && p.plant.size.z == 1).Select(p => (p.plant, p.commonality)).ToList();
        float density = settings.plantDensity < 0f ? biome.plantDensity : settings.plantDensity;
        foreach (IntVec3 cell in cells)
        {
            TerrainDef? terrain = null;
            double fertility = Noise(cell, settings.patchSize, seed, 43);
            if (waterCells.Contains(cell)) terrain = water;
            else if (settings.terrains.Count > 0)
            {
                double choice = fertility * settings.terrains.Sum(t => (double)t.weight);
                terrain = settings.terrains[settings.terrains.Count - 1].terrain;
                foreach (CQFEcologyTerrain entry in settings.terrains) { choice -= entry.weight; if (choice <= 0d) { terrain = entry.terrain; break; } }
            }
            else terrain = TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, (float)(fertility * 1.5));
            if (terrain == null) throw new CQFLayoutException("CQF_Ecology_MissingTerrain: " + biome.defName);
            plan.Terrains[cell] = terrain;
            if (terrain.IsWater || terrain.passability == Traversability.Impassable) continue;
            if (rockCells.Contains(cell)) { plan.Objects.Add(cell, rock!); plan.Rocks.Add(cell); continue; }
            if (Sample(cell.x, cell.z, seed, 71) >= density * (0.4 + Noise(cell, settings.patchSize, seed, 101))) continue;
            List<(ThingDef Plant, float Weight)> allowed = plants.Where(p => p.Plant.plant.completelyIgnoreFertility || terrain.fertility >= p.Plant.plant.fertilityMin).ToList();
            double pick = Sample(cell.x, cell.z, seed, 89) * allowed.Sum(p => (double)p.Weight);
            foreach (var candidate in allowed)
            {
                pick -= candidate.Weight;
                if (pick > 0d) continue;
                plan.Objects.Add(cell, candidate.Plant);
                if (candidate.Plant.plant.IsTree) plan.Trees.Add(cell);
                break;
            }
        }
        plan.Water.UnionWith(plan.Terrains.Where(p => p.Value.IsWater).Select(p => p.Key));
        return plan;
    }

    private double Noise(IntVec3 cell, int size, int seed, int salt)
    {
        double Layer(int scale, int layer)
        {
            double x = (double)cell.x / scale, z = (double)cell.z / scale;
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            double fx = x - ix, fz = z - iz; fx = fx * fx * (3 - 2 * fx); fz = fz * fz * (3 - 2 * fz);
            double a = Sample(ix, iz, seed, salt + layer) * (1 - fx) + Sample(ix + 1, iz, seed, salt + layer) * fx;
            double b = Sample(ix, iz + 1, seed, salt + layer) * (1 - fx) + Sample(ix + 1, iz + 1, seed, salt + layer) * fx;
            return a * (1 - fz) + b * fz;
        }
        return Layer(size, 0) * 0.7 + Layer(Math.Max(2, size / 2), 1) * 0.3;
    }

    private double Sample(int x, int z, int seed, int salt)
    {
        unchecked
        {
            uint value = (uint)(x * 374761393 + z * 668265263 + seed * 144269 + salt * 1274126177);
            value = (value ^ value >> 13) * 1274126177u; value ^= value >> 16;
            return value / 4294967296d;
        }
    }
}
