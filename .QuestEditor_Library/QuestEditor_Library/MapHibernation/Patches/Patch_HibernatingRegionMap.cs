using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch]
    public static class Patch_HibernatingRegionMap
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Region), nameof(Region.Map));
            yield return AccessTools.PropertyGetter(typeof(District), nameof(District.Map));
        }

        public static bool Prefix(object __instance, ref Map __result)
        {
            if (GameComponent_MapHibernation.Instance is not { } component)
            {
                return true;
            }
            Map map;
            bool found = __instance is Region region
                ? component.TryGetMap(region, out map)
                : component.TryGetMap((District)__instance, out map);
            if (!found)
            {
                return true;
            }
            __result = map;
            return false;
        }
    }
}
