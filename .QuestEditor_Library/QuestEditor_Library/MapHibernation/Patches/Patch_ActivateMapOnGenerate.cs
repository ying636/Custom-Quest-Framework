using System;
using System.Collections.Generic;
using System.Linq;
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
            if (GameComponent_MapManager.Instance is not { } manager) return true;
            Map map = manager.Game.FindMap(tile) ?? manager.HibernatingMaps.FirstOrDefault(candidate => candidate.Tile == tile);
            if (map == null) return true;
            __result = manager.Activate(map);
            return false;
        }
    }
}
