using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.Dispose))]
    public static class Patch_MapHibernationDispose
    {
        public static void Prefix(Game __instance)
        {
            __instance.GetComponent<GameComponent_MapHibernation>()?.DisposeHibernatingMaps();
        }
    }
}
