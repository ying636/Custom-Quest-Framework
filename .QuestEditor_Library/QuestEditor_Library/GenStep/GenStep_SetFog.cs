using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_SetFog : GenStep
    {
        public override int SeedPart => 546516544;

        public override void Generate(Map map, GenStepParams parms)
        {
			CellIndices cellIndices = map.cellIndices;
            GameTools.FogMap(map); 
            if (MapGenerator.PlayerStartSpotValid)
            {
                FloodFillerFog.FloodUnfog(MapGenerator.PlayerStartSpot, map);
            }
            if (Current.ProgramState == ProgramState.Playing)
			{
				map.roofGrid.Drawer.SetDirty();
			}
		}
    }
}
