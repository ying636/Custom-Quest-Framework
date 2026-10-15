using System;
using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
    public static class Patch_MapHibernationSave
    {
        public static void Prefix(Game __instance, out GameComponent_MapManager? __state)
        {
            __state = Scribe.mode == LoadSaveMode.Saving ? __instance.GetComponent<GameComponent_MapManager>() : null;
            __state?.BeginSave();
        }

        public static Exception? Finalizer(Exception? __exception, GameComponent_MapManager? __state)
        {
            try
            {
                __state?.EndSave();
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Failed to restore map hibernation after serialization: " + error);
                return __exception == null ? error : new AggregateException(__exception, error);
            }
            return __exception;
        }
    }
}
