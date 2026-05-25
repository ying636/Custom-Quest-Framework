using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Building), "ClaimableBy")]
    public class Patch_DisableClaim
    {
        [HarmonyPostfix]
        public static void PostFix(Building __instance,ref AcceptanceReport __result) 
        {
            if (__instance.GetLord() is Lord lord && lord.LordJob is LordJob_Custom)
            {
                __result = false;
            }
        }
    }
}
