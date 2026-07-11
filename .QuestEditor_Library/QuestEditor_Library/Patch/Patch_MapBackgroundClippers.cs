using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Map), "DrawMapClippers", MethodType.Getter)]
public class Patch_MapBackgroundClippers
{
    [HarmonyPostfix]
    public static void Postfix(Map __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        CustomMapBackgroundData background = MapComponent_CustomMapData.GetComp(__instance)?.background;
        if (background is { Enabled: true, DrawOnCameraVisibleArea: true })
        {
            __result = false;
        }
    }
}
