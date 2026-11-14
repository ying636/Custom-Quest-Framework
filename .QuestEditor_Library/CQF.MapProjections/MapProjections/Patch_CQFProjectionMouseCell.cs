#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(UI), nameof(UI.MouseCell))]
public static class Patch_CQFProjectionMouseCell
{
    public static void Postfix(ref IntVec3 __result)
    {
        if (CQFMapProjectionContext.Current is not { } context) return;
        if (!context.AllowsOutsidePointer && !context.PointerInside)
        {
            __result = IntVec3.Invalid;
            return;
        }
        Rect bounds = context.Projection.ViewRect;
        __result.x = Mathf.Clamp(__result.x, Mathf.FloorToInt(bounds.xMin), Mathf.FloorToInt(bounds.xMax - 0.001f));
        __result.z = Mathf.Clamp(__result.z, Mathf.FloorToInt(bounds.yMin), Mathf.FloorToInt(bounds.yMax - 0.001f));
    }
}
#endif
