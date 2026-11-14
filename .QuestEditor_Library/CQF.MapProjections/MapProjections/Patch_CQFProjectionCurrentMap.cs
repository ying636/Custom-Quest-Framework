#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Game), nameof(Game.CurrentMap), MethodType.Getter)]
public static class Patch_CQFProjectionCurrentMap
{
    public static void Postfix(Game __instance, ref Map __result)
    {
        if (ReferenceEquals(__instance, Current.Game) && CQFMapProjectionContext.Current is { } context)
            __result = context.Projection.sourceMap!;
    }
}
#endif
