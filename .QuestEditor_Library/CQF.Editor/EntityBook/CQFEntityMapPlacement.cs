using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEntityMapPlacement
    {
        public static void Begin(PawnSpawnData recipe, Quest? quest = null)
        {
            Map map = Find.CurrentMap ?? throw new InvalidOperationException("CQF_EntityBook_MapRequired".Translate());
            if (Current.Game == null || !DebugSettings.godMode)
                throw new InvalidOperationException("CQF_EntityBook_MapRequired".Translate());
            if (!recipe.CanSaveToMap()) throw new InvalidOperationException("CQF_EntityBook_InvalidRecipe".Translate());
            PawnSpawnData working = CQFEntityRecipeStore.Copy(recipe);
            CQFEntityBookCatalog.Track(working);
            Find.DesignatorManager.Select(new Designator_CQFEntityPlacement(working, map, quest));
        }

        public static AcceptanceReport CanPlace(IntVec3 cell, Map map) => CanPlace(cell, map, null);

        public static AcceptanceReport CanPlace(IntVec3 cell, Map map, PawnSpawnData? recipe, Spawner? existingMarker = null)
        {
            if (Current.Game == null || !DebugSettings.godMode || !ReferenceEquals(Find.CurrentMap, map) || map.Disposed)
                return "CQF_EntityBook_MapRequired".Translate().ToString();
            if (!cell.InBounds(map))
                return "CQF_EntityBook_PlacementBlocked".Translate().ToString();
            if (cell.GetThingList(map).Any(thing => thing is Spawner && !ReferenceEquals(thing, existingMarker)))
                return "CQF_EntityBook_PlacementOccupied".Translate().ToString();
            if (recipe?.spawnType is SpawnType.BuildingDamaged or SpawnType.BuildingDestroyed)
                return cell.GetFirstBuilding(map) is Building { Destroyed: false } ? true : "CQF_EntityBook_PlacementBuildingRequired".Translate().ToString();
            if (!cell.Walkable(map)) return "CQF_EntityBook_PlacementBlocked".Translate().ToString();
            if (cell.GetThingList(map).Any(thing => thing.def.category == ThingCategory.Building))
                return "CQF_EntityBook_PlacementOccupied".Translate().ToString();
            return true;
        }

        public static Spawner Place(PawnSpawnData recipe, IntVec3 cell, Map map)
        {
            AcceptanceReport report = CanPlace(cell, map, recipe);
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            if (!recipe.CanSaveToMap()) throw new InvalidOperationException("CQF_EntityBook_InvalidRecipe".Translate());
            PawnSpawnData placed = CQFEntityRecipeStore.Copy(recipe);
            Spawner marker = (Spawner)ThingMaker.MakeThing(QEDefOf.QE_Spawner_Editor);
            marker.pawns.Add(placed);
            if (GenSpawn.Spawn(marker, cell, map, WipeMode.VanishOrMoveAside) is not Spawner spawned)
                throw new InvalidOperationException("CQF_EntityBook_PlacementFailed".Translate());
            CQFEntityBookCatalog.Track(placed);
            return spawned;
        }
    }
}
