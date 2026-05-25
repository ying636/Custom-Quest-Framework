using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using Verse.AI;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(CompLaunchable), "AnyInGroupIsUnderRoof" , MethodType.Getter)]
    public class Patch_DisableLaunch
    {
        [HarmonyPostfix]
        public static void postfix(CompLaunchable __instance, ref bool __result)
        {
            if (__instance.parent.Map.Parent is MapParent_Custom)
            {
                __result = true;
                return;
            }
        }
    }
}