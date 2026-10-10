using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch]
    public static class Patch_HibernatingMapTick
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Map), nameof(Map.MapPreTick));
            yield return AccessTools.Method(typeof(Map), nameof(Map.MapPostTick));
            yield return AccessTools.Method(typeof(Map), nameof(Map.MapUpdate));
        }

        public static bool Prefix(Map __instance)
        {
            return GameComponent_MapHibernation.Instance?.IsHibernating(__instance) != true;
        }
    }
}
