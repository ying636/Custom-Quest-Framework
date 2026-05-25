using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Building), "PreApplyDamage")]
    public class Patch_BuildingDamaged
    {
        [HarmonyPostfix]
        public static void PostFix(Building __instance, DamageInfo dinfo)
        {
            if (__instance.Spawned && __instance.Map.GetComponent<MapComponent_CustomMapData>()
                is MapComponent_CustomMapData component)
            {
                component.Notify_ThingDamaged(__instance, dinfo);
            }
        }
    }
    [HarmonyPatch(typeof(Building), "Destroy")]
    public class Patch_BuildingDestroy
    {
        [HarmonyPrefix]
        public static bool PreFix(Building __instance)
        {
            if (__instance.Spawned && __instance.Map.GetComponent<MapComponent_CustomMapData>() 
                is MapComponent_CustomMapData component && component.PawnSpawnDatas_Building.TryGetValue(__instance, out List<PawnSpawnData> list))
            {
                foreach (PawnSpawnData data in list)
                {
                    if (data.spawnType == SpawnType.BuildingDestroyed)
                    {
                        if (component.TryGetLord(data.lordDataName, out Lord lord))
                        {
                            data.Spawn(__instance.Position, __instance.Map, component.QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == component.QuestTag), lord);
                        }
                        else
                        {
                            data.Spawn(__instance.Position, __instance.Map, component.QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == component.QuestTag),null,false);
                        }
                    }
                }
            }
            return true;
        }
    }
}