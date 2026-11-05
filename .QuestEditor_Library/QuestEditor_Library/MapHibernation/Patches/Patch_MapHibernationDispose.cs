using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.Dispose))]
    public static class Patch_MapHibernationDispose
    {
        public static void Prefix(Game __instance)
        {
            try { __instance.GetComponent<GameComponent_MapManager>()?.DisposeMaps(); }
            catch (Exception error) { Log.Error("[CQF] Map cleanup failed while disposing the game: " + error); }
        }
    }
}
