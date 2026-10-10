using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.CurrentMap), MethodType.Setter)]
    public static class Patch_ActivateMapOnSelect
    {
        public static void Prefix(Game __instance, Map value)
        {
            GameComponent_MapHibernation component = __instance.GetComponent<GameComponent_MapHibernation>();
            if (component?.IsHibernating(value) == true)
            {
                component.Activate(value);
            }
        }
    }
}
