using HarmonyLib;
using RimWorld;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
    public static class Patch_HibernatingColonistsGameOver
    {
        public static bool Prefix(GameEnder __instance)
        {
            if (GameComponent_MapHibernation.Instance?.HasHibernatingColonists == true)
            {
                __instance.gameEnding = false;
                return false;
            }
            return true;
        }
    }
}
