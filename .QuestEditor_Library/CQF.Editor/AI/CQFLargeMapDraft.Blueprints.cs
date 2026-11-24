using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFLargeMapDraft
    {
        public static IReadOnlyList<CustomMapDataDef> Blueprints() => BlueprintRepository.AllBlueprints
            .Concat(DefDatabase<CustomMapDataDef>.AllDefsListForReading.Where(def => def.isPart))
            .Distinct().OrderBy(def => def.defName, StringComparer.Ordinal).ToArray();

        private IEnumerable<int> StageBlueprint(XElement op)
        {
            Validate(op, new[] { "name", "x", "z", "rotation", "repeatX", "repeatZ", "stepX", "stepZ" });
            string name = Required(op, "name");
            var candidates = Blueprints().Where(def => def.defName == name).ToArray();
            if (candidates.Length != 1) throw new InvalidDataException("CQF_AI_MissingResource: " + name);
            CustomMapDataDef source = candidates[0];
            if (source.customThings?.Count > 0 || source.zoneCores?.Count > 0 || source.pawns?.Count > 0 || source.specialSpawnPawns?.Count > 0 ||
                source.lordDatas?.Count > 0 || source.routes?.Count > 0 || source.terrainsColorRect?.Count > 0 ||
                source.thingDatas.Any(data => data.GetType() != typeof(ThingData) || data.targetKeys?.Count > 0))
                throw new InvalidDataException("CQF_AI_LargeDraftBlueprintComplex");
            int rotation = Number(op, "rotation", source.rot.IsValid ? source.rot.AsInt : 0);
            int repeatX = Number(op, "repeatX", 1), repeatZ = Number(op, "repeatZ", 1);
            if (rotation < 0 || rotation > 3 || repeatX < 1 || repeatZ < 1 || (long)repeatX * repeatZ > 256)
                throw new InvalidDataException("CQF_AI_InvalidChanges: blueprint rotation or repetition");
            if (!source.rot.IsValid)
            {
                source = source.Copy("_DraftRotation");
                source.rot = Rot4.North;
            }
            if (source.rot.AsInt != rotation) source = source.GetRotated(new Rot4(rotation));
            int x = Number(op, "x"), z = Number(op, "z");
            int stepX = Number(op, "stepX", source.size.x), stepZ = Number(op, "stepZ", source.size.z);
            for (int row = 0; row < repeatZ; row++)
                for (int column = 0; column < repeatX; column++)
                {
                    long offsetX = x + (long)stepX * column, offsetZ = z + (long)stepZ * row;
                    if (offsetX < 0 || offsetZ < 0 || offsetX >= Map.Size.x || offsetZ >= Map.Size.z)
                        throw new InvalidDataException("CQF_AI_MapBounds");
                    var offset = new IntVec3((int)offsetX, 0, (int)offsetZ);
                    foreach (var entry in source.terrains)
                        foreach (IntVec3 cell in entry.Value) { PaintBlueprintTerrain(offset + cell, TerrainNamed(entry.Key)); yield return 0; }
                    foreach (var entry in source.terrainsRect)
                    {
                        TerrainDef def = TerrainNamed(entry.Key);
                        foreach (CellRect rect in entry.Value)
                            foreach (IntVec3 cell in rect) { PaintBlueprintTerrain(offset + cell, def); yield return 0; }
                    }
                    foreach (var entry in source.roofs)
                        foreach (IntVec3 cell in entry.Value) { PaintBlueprintRoof(offset + cell, entry.Key); yield return 0; }
                    foreach (var entry in source.roofRects)
                        foreach (CellRect rect in entry.Value)
                            foreach (IntVec3 cell in rect) { PaintBlueprintRoof(offset + cell, entry.Key); yield return 0; }
                    foreach (ThingData data in source.thingDatas)
                        foreach (IntVec3 cell in CQFAIMapPlan.Positions(data))
                        {
                            if (placements.Count >= 4096) throw new InvalidDataException("CQF_AI_ToolLimit");
                            ThingData placement = data.Copy();
                            placement.position = offset + cell;
                            placement.allPositions.Clear();
                            placement.allRect.Clear();
                            placements.Add(placement);
                            yield return 0;
                        }
                }
        }

        private void PaintBlueprintTerrain(IntVec3 cell, TerrainDef def)
        {
            if (!cell.InBounds(Map)) throw new InvalidDataException("CQF_AI_MapBounds");
            Paint(cell, def);
        }

        private void PaintBlueprintRoof(IntVec3 cell, RoofDef def)
        {
            if (!cell.InBounds(Map)) throw new InvalidDataException("CQF_AI_MapBounds");
            roofs[cell] = def;
        }

        private void CheckPlacements()
        {
            placementConflicts.Clear();
            blockedPlacements.Clear();
            HashSet<IntVec3> occupied = new HashSet<IntVec3>();
            foreach (ThingData data in placements)
            {
                CellRect footprint = GenAdj.OccupiedRect(data.position, data.rotation, data.def.size);
                if (!footprint.InBounds(Map)) throw new InvalidDataException("CQF_AI_MapBounds");
                foreach (IntVec3 cell in footprint)
                {
                    if (!occupied.Add(cell) || cell.GetThingList(Map).Any(thing => BlocksPlacement(data.def, thing)))
                        placementConflicts.Add(cell);
                    if (data.def.passability == Traversability.Impassable) blockedPlacements.Add(cell);
                }
            }
        }

        private static bool BlocksPlacement(ThingDef requested, Thing existing)
        {
            ThingDef current = existing.def;
            if (current.IsBlueprint || current.IsFrame || existing is Pawn && requested.passability == Traversability.Impassable)
                return true;
            if (requested.category == current.category && current.category is ThingCategory.Item or ThingCategory.Plant)
                return true;
            return current.category is not (ThingCategory.Plant or ThingCategory.Filth)
                && (GenSpawn.SpawningWipes(requested, current) || requested.IsEdifice() && current.IsEdifice());
        }

        private HashSet<IntVec3> placementConflicts = new HashSet<IntVec3>();
    }
}
