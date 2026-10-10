using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.RegisterAllTickabilityFor))]
    public static class Patch_HibernatingTickRegistration
    {
        public static bool Prefix(Thing t)
        {
            if (GameComponent_MapHibernation.Instance is not { HasHibernatingMaps: true } component)
            {
                return true;
            }
            return !component.TryGetMap(t, out _) && (t.Spawned || !component.IsHibernating(t.MapHeld));
        }
    }
}
