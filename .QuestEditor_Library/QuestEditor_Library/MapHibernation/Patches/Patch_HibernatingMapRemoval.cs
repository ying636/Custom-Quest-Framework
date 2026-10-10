using HarmonyLib;
using RimWorld.Planet;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.CheckRemoveMapNow))]
    public static class Patch_HibernatingMapRemoval
    {
        public static bool Prefix(MapParent __instance)
        {
            return GameComponent_MapHibernation.Instance is not { } component || !component.TryGetMap(__instance, out _);
        }
    }
}
