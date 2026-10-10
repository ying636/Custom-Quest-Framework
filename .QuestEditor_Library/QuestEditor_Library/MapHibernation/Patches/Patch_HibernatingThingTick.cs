using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DoTick))]
    public static class Patch_HibernatingThingTick
    {
        public static bool Prefix(Thing __instance)
        {
            if (GameComponent_MapHibernation.Instance is not { HasHibernatingMaps: true } component)
            {
                return true;
            }
            return !component.TryGetMap(__instance, out _) &&
                (__instance.Spawned || !component.IsHibernating(__instance.MapHeld));
        }
    }
}
