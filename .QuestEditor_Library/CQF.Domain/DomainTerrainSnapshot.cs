using Verse;

namespace QuestEditor_Library;

public sealed class DomainTerrainSnapshot : IExposable
{
    public DomainTerrainSnapshot() { }

    public DomainTerrainSnapshot(Map source)
    {
        width = source.Size.x;
        height = source.Size.z;
        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++) terrains.Add(source.terrainGrid.TerrainAt(new IntVec3(x, 0, z)));
    }

    public TerrainDef At(IntVec3 cell, IntVec3 destinationSize)
    {
        int x = Math.Min(width - 1, (int)((long)cell.x * width / destinationSize.x));
        int z = Math.Min(height - 1, (int)((long)cell.z * height / destinationSize.z));
        return terrains[z * width + x];
    }

    public void Restore(Map map)
    {
        foreach (IntVec3 cell in map.AllCells)
        {
            TerrainDef terrain = At(cell, map.Size);
            if (map.terrainGrid.TerrainAt(cell) != terrain) map.terrainGrid.SetTerrain(cell, terrain);
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref width, "width");
        Scribe_Values.Look(ref height, "height");
        Scribe_Collections.Look(ref terrains, "terrains", LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit
            && (width <= 0 || height <= 0 || terrains == null || terrains.Count != (long)width * height || terrains.Any(terrain => terrain == null)))
            throw new InvalidOperationException("[CQF] Invalid saved domain terrain snapshot.");
    }

    private int width;
    private int height;
    private List<TerrainDef> terrains = new();
}
