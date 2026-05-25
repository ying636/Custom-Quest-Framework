using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(ExitMapGrid), "MapUsesExitGrid", MethodType.Getter)]
    public class Patch_MapExit
    {
        [HarmonyPostfix]
        public static void postfix(ExitMapGrid __instance, ref bool __result, Map ___map)
        {
            if (___map.Parent is MapParent_Custom)
            {
                __result = false;
            }
        }
    }
}
