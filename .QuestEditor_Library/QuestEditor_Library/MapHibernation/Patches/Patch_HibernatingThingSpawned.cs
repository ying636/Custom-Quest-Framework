using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Spawned), MethodType.Getter)]
    public static class Patch_HibernatingThingSpawned
    {
        public static bool Prefix(Thing __instance, ref bool __result)
        {
            if (GameComponent_MapHibernation.Instance is { } component && component.TryGetMap(__instance, out _))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
