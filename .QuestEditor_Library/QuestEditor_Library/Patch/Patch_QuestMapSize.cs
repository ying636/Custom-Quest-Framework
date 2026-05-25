using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using Verse.AI;
using RimWorld.Planet;

namespace QuestEditor_Library
{

    [HarmonyPatch(typeof(MapGenerator), "GenerateContentsIntoMap")]
    public class Patch_ExtraGenStepDefs
    {
        [HarmonyPrefix]
        public static bool postfix(ref IEnumerable<GenStepWithParams> genStepDefs)
        {
            if (genStepDefs.ToList().Find(p0 => p0.parms.sitePart?.parms is CustomSitePartParams) is GenStepWithParams p && p.parms.sitePart is SitePart part)
            {
                genStepDefs = get(genStepDefs);
            }
            return true;
        }

        public static IEnumerable<GenStepWithParams> get(IEnumerable<GenStepWithParams> genStepDefs) 
        {
            foreach (GenStepWithParams p in genStepDefs) 
            {
                if (p.def.defName != "SteamGeysers") 
                {
                    yield return p;
                }
            }
            yield break;
        }
    }
}
