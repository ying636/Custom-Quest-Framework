using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(GetOrGenerateMapUtility), nameof(GetOrGenerateMapUtility.GetOrGenerateMap),
        new[] { typeof(PlanetTile), typeof(IntVec3), typeof(WorldObjectDef), typeof(IEnumerable<GenStepWithParams>), typeof(bool) })]
    public static class Patch_ActivateMapOnGenerate
    {
        public static bool Prefix(PlanetTile tile, ref Map __result)
        {
            if (GameComponent_MapHibernation.Instance is { } component && component.FindHibernatingMap(tile) is { } map)
            {
                __result = component.Activate(map);
                return false;
            }
            return true;
        }
    }
}
