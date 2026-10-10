using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.Map), MethodType.Getter)]
    public static class Patch_HibernatingMapParent
    {
        public static void Postfix(MapParent __instance, ref Map __result)
        {
            if (__result == null && GameComponent_MapHibernation.Instance is { } component &&
                component.TryGetMap(__instance, out Map map))
            {
                __result = map;
            }
        }
    }
}
