using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Patch_ActivateMapOnDespawn
    {
        public static void Prefix(Thing __instance)
        {
            if (GameComponent_MapHibernation.Instance is { } component && component.TryGetMap(__instance, out Map map))
            {
                component.Activate(map);
            }
        }
    }
}
