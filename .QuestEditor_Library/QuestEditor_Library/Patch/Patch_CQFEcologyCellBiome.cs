#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(MixedBiomeMapComponent), nameof(MixedBiomeMapComponent.GetBiomeAt))]
public static class Patch_CQFEcologyCellBiome
{
    public static void Postfix(MixedBiomeMapComponent __instance, IntVec3 cell, ref BiomeDef __result)
    {
        if (cell.InBounds(__instance.map) && __instance.map.GetComponent<MapComponent_CQFEcology>()?.Biome is BiomeDef biome) __result = biome;
    }
}
#endif
