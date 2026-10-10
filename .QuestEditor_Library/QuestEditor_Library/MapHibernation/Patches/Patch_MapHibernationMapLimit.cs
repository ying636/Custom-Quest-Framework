using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.AddMap))]
    public static class Patch_MapHibernationMapLimit
    {
        public static void Prefix(Game __instance, Map map)
        {
            __instance.GetComponent<GameComponent_MapHibernation>()?.CheckCanAddMap(map);
        }
    }
}
