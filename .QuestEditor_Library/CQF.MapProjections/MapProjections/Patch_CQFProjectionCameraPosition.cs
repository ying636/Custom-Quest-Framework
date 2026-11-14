#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraDriver), "CurrentRealPosition", MethodType.Getter)]
public static class Patch_CQFProjectionCameraPosition
{
    public static void Postfix(ref Vector3 __result)
    {
        if (CQFMapProjectionContext.Current?.RenderCamera is { } camera) __result = camera.transform.position;
    }
}
#endif
