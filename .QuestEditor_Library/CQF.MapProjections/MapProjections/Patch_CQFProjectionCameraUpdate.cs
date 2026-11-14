#if RIMWORLD_1_7
using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.Update))]
public static class Patch_CQFProjectionCameraUpdate
{
    public static bool Prefix(CameraDriver __instance) => __instance != Current.CameraDriver || !CQFMapProjectionNavigation.HoldsHostCamera;
}
#endif
