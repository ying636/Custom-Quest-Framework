#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Find), nameof(Find.Camera), MethodType.Getter)]
public static class Patch_CQFProjectionCamera
{
    public static void Postfix(ref Camera __result)
    {
        if (CQFMapProjectionContext.Current is { } context) __result = (context.RenderCamera ?? context.InputCamera)!;
    }
}
#endif
