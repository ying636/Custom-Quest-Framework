using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.CurrentMap), MethodType.Setter)]
    public static class Patch_ActivateMapOnSelect
    {
        public static void Prefix(Game __instance, Map value)
        {
            GameComponent_MapManager component = __instance.GetComponent<GameComponent_MapManager>();
            if (component != null && (component.IsHibernating(value) || component.IsHibernateRequested(value)))
            {
                component.Activate(value);
            }
        }
    }
}
