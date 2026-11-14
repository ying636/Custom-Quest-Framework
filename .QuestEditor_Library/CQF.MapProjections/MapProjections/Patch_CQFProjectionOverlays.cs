#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate))]
public static class Patch_CQFProjectionOverlays
{
    public static bool Prefix()
    {
        if (CQFMapProjectionContext.IsRendering || CQFMapProjectionContext.Focused == null) return true;
        Map? host = CQFMapProjectionContext.HostMap;
        if (host != null) MapComponentUtility.MapComponentOnDraw(host);
        return false;
    }
}
#endif
