#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI))]
public static class Patch_CQFProjectionCameraInput
{
    public static bool Prefix(CameraDriver __instance, ref Vector2 ___desiredDolly, ref Vector2 ___desiredDollyRaw, ref Vector3 ___velocity)
    {
        if (__instance != Current.CameraDriver || !CQFMapProjectionNavigation.BlocksHostCamera) return true;
        ___desiredDolly = Vector2.zero;
        ___desiredDollyRaw = Vector2.zero;
        ___velocity = Vector3.zero;
        return false;
    }
}
#endif
