using Verse;

namespace QuestEditor_Library;

public sealed class GenStep_TestDomain : GenStep
{
    public override int SeedPart => 63610361;

    public override void Generate(Map map, GenStepParams parms)
    {
        foreach (IntVec3 cell in map.AllCells)
        {
            map.roofGrid.SetRoof(cell, null);
            map.terrainGrid.SetTerrain(cell, cell.x % 5 == 0 || cell.z % 5 == 0 ? gridTerrain : terrain);
        }
        map.fogGrid.ClearAllFog();
        MapGenerator.PlayerStartSpot = map.Center;
    }

    public TerrainDef terrain = null!;
    public TerrainDef gridTerrain = null!;
}
