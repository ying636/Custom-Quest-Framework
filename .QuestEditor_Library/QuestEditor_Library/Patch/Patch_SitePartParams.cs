using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using RimWorld.Planet;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(SitePartParams), "ExposeData")]
    public class Patch_SitePartParams
    {
        [HarmonyPostfix]
        static void PostFix(SitePartParams __instance)
        {
            if (__instance is CustomSitePartParams customParams) 
            {
                Scribe_Values.Look(ref customParams.replaceMapGeneration, "QE_CustomParams_replaceMapGeneration");
                Scribe_Values.Look(ref customParams.isSubMap, "QE_CustomParams_isSubMap");
                Scribe_Defs.Look(ref customParams.mapData, "QE_CustomParams_mapData");
                Scribe_References.Look(ref customParams.quest, "QE_CustomParams_quest");
            }
        }
    }
}