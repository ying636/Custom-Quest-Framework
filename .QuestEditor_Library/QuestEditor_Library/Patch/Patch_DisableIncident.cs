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
    [HarmonyPatch(typeof(IncidentWorker), "CanFireNow")]
    public class Patch_DisableIncident
    {
        [HarmonyPrefix]
        public static bool prefix(IncidentWorker __instance, ref bool __result, IncidentParms parms)
        {
            if (parms.target is Map map && map.Parent is MapParent_Custom parent && (__instance.def.tags == null || !__instance.def.tags.Exists(t => parent.tags.Contains(t) || (parent.mapDataDef is CustomMapDataDef def && def.tags.Contains(t)))))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}