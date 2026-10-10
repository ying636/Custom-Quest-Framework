using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch]
    public static class Patch_MapHibernationExecution
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick));
            yield return AccessTools.Method(typeof(Game), nameof(Game.UpdatePlay));
        }

        public static void Prefix(out GameComponent_MapHibernation? __state)
        {
            __state = GameComponent_MapHibernation.Instance;
            __state?.BeginExecution();
        }

        public static Exception? Finalizer(Exception? __exception, GameComponent_MapHibernation? __state)
        {
            try
            {
                __state?.EndExecution(__exception == null);
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Failed to complete map hibernation requests: " + error);
                return __exception == null ? error : new AggregateException(__exception, error);
            }
            return __exception;
        }
    }
}
