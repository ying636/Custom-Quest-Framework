#if RIMWORLD_1_7
using System.Reflection;
using HarmonyLib;

namespace QuestEditor_Library;

[HarmonyPatch]
public static class Patch_CQFProjectionCameraPlusInsets
{
    public static bool Prepare() => AccessTools.TypeByName("CameraPlus.EdgeUIInsets") != null;

    public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("CameraPlus.EdgeUIInsets"), "ObserveMarker")
        ?? throw new MissingMethodException("CameraPlus.EdgeUIInsets", "ObserveMarker");

    public static bool Prefix() => !CQFMapProjectionContext.IsRendering;
}
#endif
