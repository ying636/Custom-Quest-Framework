using HarmonyLib;
using RimWorld.Planet;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.DoTick))]
    public static class Patch_HibernatingWorldObjectTick
    {
        public static bool Prefix(WorldObject __instance)
        {
            return __instance is not MapParent parent || GameComponent_MapHibernation.Instance is not { } component ||
                !component.TryGetMap(parent, out _);
        }
    }
}
