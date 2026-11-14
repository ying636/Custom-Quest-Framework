#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
public static class Patch_CQFProjectionGUI
{
    public static void Prefix(out CQFMapProjectionContext? __state) => __state = CQFMapProjectionContext.BeginGUI();

    public static Exception? Finalizer(Exception? __exception, CQFMapProjectionContext? __state)
    {
        __state?.Dispose();
        return __exception;
    }
}
#endif
