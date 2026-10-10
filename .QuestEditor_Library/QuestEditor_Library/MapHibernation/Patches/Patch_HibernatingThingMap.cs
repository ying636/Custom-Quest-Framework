using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Map), MethodType.Getter)]
    public static class Patch_HibernatingThingMap
    {
        public static bool Prefix(Thing __instance, ref Map __result)
        {
            if (GameComponent_MapHibernation.Instance is { } component && component.TryGetMap(__instance, out Map map))
            {
                __result = map;
                return false;
            }
            return true;
        }
    }
}
