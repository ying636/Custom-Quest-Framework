using RimWorld;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class GenStep_LayoutChecks : GenStep
{
    public override int SeedPart => 63610372;

    public override void Generate(Map map, GenStepParams parms)
    {
        foreach (IntVec3 cell in map.AllCells)
        {
            map.roofGrid.SetRoof(cell, null);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.PavedTile);
        }
        foreach (int x in new[] { 12, 46 })
        {
            Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
            wall.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(wall, new IntVec3(x, 0, 28), map);
            Thing bed = ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            bed.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(bed, new IntVec3(x + 2, 0, 34), map);
        }
        Thing casket = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CryptosleepCasket"));
        casket.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(casket, new IntVec3(16, 0, 40), map);
        map.fogGrid.ClearAllFog();
        MapGenerator.PlayerStartSpot = map.Center;
    }
}
