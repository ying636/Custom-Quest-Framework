using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter),
        new[]
        {
            typeof(Caravan),
            typeof(Map),
            typeof(CaravanEnterMode),
            typeof(CaravanDropInventoryMode),
            typeof(bool),
            typeof(Predicate<IntVec3>)
        })]
    public static class Patch_CaravanEnterMap
    {
        public static bool Prefix(Caravan caravan, Map map, CaravanEnterMode enterMode,
            CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
            bool draftColonists = false, Predicate<IntVec3> extraCellValidator = null)
        {
            if (enterMode != CaravanEnterMode.Edge || !TryGetCustomMapData(map, out CustomMapDataDef def) ||
                !def.TryGetEnterSpot(map, out IntVec3 enterSpot))
            {
                return true;
            }
            CaravanEnterMapUtility.Enter(caravan, map,
                pawn => CellFinder.RandomSpawnCellForPawnNear(enterSpot, map),
                dropInventoryMode, draftColonists);
            return false;
        }

        private static bool TryGetCustomMapData(Map map, out CustomMapDataDef def)
        {
            def = null;
            if (map?.Parent is CustomSite site)
            {
                def = site.mapDef;
            }
            else if (map?.Parent is MapParent_Custom custom)
            {
                def = custom.mapDataDef;
            }
            return def != null;
        }
    }
}
