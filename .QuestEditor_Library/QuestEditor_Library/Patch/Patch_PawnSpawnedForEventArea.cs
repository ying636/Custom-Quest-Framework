using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public class Patch_PawnSpawnedForEventArea
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, Map map)
        {
            if (map?.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component)
            {
                component.Notify_PawnSpawned(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "DeSpawn")]
    public class Patch_PawnDespawnedForEventArea
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn __instance)
        {
            if (__instance.Spawned && __instance.Map?.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component)
            {
                component.Notify_PawnDespawned(__instance);
            }
        }
    }
}
