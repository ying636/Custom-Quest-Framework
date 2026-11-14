using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Map), nameof(Map.Biome), MethodType.Getter)]
public static class Patch_CQFEcologyBiome
{
    public static void Postfix(Map __instance, ref BiomeDef __result)
    {
        if (__instance.GetComponent<MapComponent_CQFEcology>()?.Biome is BiomeDef biome) __result = biome;
    }
}
