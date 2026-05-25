using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_SetTerrain : GenStep
    {
        public override int SeedPart => 131340;

        public override void Generate(Map map, GenStepParams parms)
        {
            TerrainDef nullDef = TerrainDef.Named("QE_Null");
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (IntVec3 c in map.AllCells)
			{
                terrainGrid.SetTerrain(c, nullDef);
			}          
            foreach (TerrainPatchMaker terrainPatchMaker in map.Biome.terrainPatchMakers)
            {
                terrainPatchMaker.Cleanup();
            }
            if (GenStep_SetTerrain.customMap != null) 
            {
                GenStep_CustomMap.SetRoofAndTerrain(map, customMap,IntVec3.Zero);
                customMap = null;
            }
        }
        public static CustomMapDataDef customMap;
    }
}
