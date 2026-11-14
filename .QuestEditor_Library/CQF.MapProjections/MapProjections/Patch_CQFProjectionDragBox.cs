#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(DragBox), nameof(DragBox.ScreenRect), MethodType.Getter)]
public static class Patch_CQFProjectionDragBox
{
    public static void Postfix(ref Rect __result)
    {
        if (CQFMapProjectionContext.Current is not { } context) return;
        Rect bounds = context.Projection.ScreenRect(context.HostCamera);
        __result = Rect.MinMaxRect(Mathf.Clamp(__result.xMin, bounds.xMin, bounds.xMax), Mathf.Clamp(__result.yMin, bounds.yMin, bounds.yMax),
            Mathf.Clamp(__result.xMax, bounds.xMin, bounds.xMax), Mathf.Clamp(__result.yMax, bounds.yMin, bounds.yMax));
    }
}
#endif
