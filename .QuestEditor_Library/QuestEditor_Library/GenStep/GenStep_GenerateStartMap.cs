using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_GenerateStartMap : GenStep
    {
        public override int SeedPart => 1919;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (Find.Scenario.AllParts.ToList().Find(p => p is ScenPart_GenerateCustomMap) is ScenPart_GenerateCustomMap part) 
            {
                GenStep_CustomMap.SpawnCustomMap(map, parms,part.map, null, false,IntVec3.Zero, false, false, false);
            }
        }
    }
}
