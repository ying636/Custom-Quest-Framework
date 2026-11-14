#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
public static class Patch_CQFProjectionView
{
    public static bool Prefix(ref CellRect __result)
    {
        if (CQFMapProjectionContext.Current is not { } context) return true;
        __result = context.Projection.VisibleCells;
        return false;
    }
}
#endif
