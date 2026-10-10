using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
    public static class Patch_ActivateMapOnRemove
    {
        public static void Prefix(Game __instance, Map map)
        {
            GameComponent_MapHibernation component = __instance.GetComponent<GameComponent_MapHibernation>();
            if (component != null && (component.IsHibernating(map) || component.IsHibernateRequested(map)))
            {
                component.Activate(map);
            }
        }
    }
}
