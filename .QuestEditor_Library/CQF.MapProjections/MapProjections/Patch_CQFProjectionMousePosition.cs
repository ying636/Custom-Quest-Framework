#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(UI), nameof(UI.MouseMapPosition))]
public static class Patch_CQFProjectionMousePosition
{
    public static void Postfix(ref Vector3 __result)
    {
        if (CQFMapProjectionContext.Current is not { } context) return;
        if (!context.AllowsOutsidePointer && !context.PointerInside)
        {
            __result = new Vector3(-100000f, 0f, -100000f);
            return;
        }
        Rect bounds = context.Projection.ViewRect;
        __result = new Vector3(Mathf.Clamp(__result.x, bounds.xMin, bounds.xMax - 0.001f), 0f,
            Mathf.Clamp(__result.z, bounds.yMin, bounds.yMax - 0.001f));
    }
}
#endif
