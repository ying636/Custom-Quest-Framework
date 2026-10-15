using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Patch_ActivateMapOnDespawn
    {
        public static void Prefix(Thing __instance)
        {
            if (GameComponent_MapManager.Instance is not { } component) return;
            if (component.TryGetMap(__instance, out Map map))
            {
                component.Activate(map);
            }
            else if (__instance.Map is { } activeMap && component.IsHibernateRequested(activeMap))
            {
                component.Activate(activeMap);
            }
        }
    }
}
