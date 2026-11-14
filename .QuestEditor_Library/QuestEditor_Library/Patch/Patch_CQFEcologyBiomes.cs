#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Map), nameof(Map.Biomes), MethodType.Getter)]
public static class Patch_CQFEcologyBiomes
{
    public static void Postfix(Map __instance, ref IEnumerable<BiomeDef> __result)
    {
        if (__instance.GetComponent<MapComponent_CQFEcology>()?.Biome is BiomeDef biome) __result = new[] { biome };
    }
}
#endif
