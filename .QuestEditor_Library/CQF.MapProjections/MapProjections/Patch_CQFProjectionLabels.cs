#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(UI), nameof(UI.MapToUIPosition))]
public static class Patch_CQFProjectionLabels
{
    public static void Postfix(Vector3 v, ref Vector2 __result)
    {
        if (CQFMapProjectionContext.Current is not { } context) return;
        Vector3 point = context.HostCamera.WorldToScreenPoint(context.Projection.ToHost(v)) / Prefs.UIScale;
        __result = new Vector2(point.x, UI.screenHeight - point.y);
    }
}
#endif
