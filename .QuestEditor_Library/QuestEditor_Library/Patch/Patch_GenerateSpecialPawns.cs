using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using Verse.AI;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(IncidentWorker_NeutralGroup), "SpawnPawns")]
    class Patch_GenerateSpecialPawns
    {
        [HarmonyPostfix]
        public static void postfix(List<Pawn> __result)
        {
            DefDatabase<SpecialPawnGenerateDef>.AllDefsListForReading.RandomElementByWeight(d =>
                d.commonality)?.generator?.Work(__result);
        }
    }
}