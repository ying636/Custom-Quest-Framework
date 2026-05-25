using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(MapGenerator), "GenerateMap")]
    public static class Patch_MapGenerate
    {
        [HarmonyPrefix]
        public static bool prefix(ref IntVec3 mapSize, MapParent parent
            , MapGeneratorDef mapGenerator)
        { 
            if (mapGenerator == QEDefOf.CQF_Base_Player
                &&
                Find.Scenario.AllParts.ToList().Find(p => p is ScenPart_GenerateCustomMap) 
                is ScenPart_GenerateCustomMap part) 
            {
                mapSize = part.map.size; 
            }
            if (parent is CustomSite site) 
            {
                if (site.mapDef is {} def)
                {
                    if ((def.size.z > mapSize.z || def.size.x > mapSize.x))
                    {
                        mapSize.x = Math.Max(mapSize.x, def.size.x);
                        mapSize.z = Math.Max(mapSize.z, def.size.z);
                    }
                    if (site.replaceMapGeneration)
                    {
                        mapSize.x = def.size.x;
                        mapSize.z = def.size.z;
                    } 
                }
            }
            return true;
        }

    }
}
