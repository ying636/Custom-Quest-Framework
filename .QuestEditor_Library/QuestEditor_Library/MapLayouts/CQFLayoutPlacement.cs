using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutPlacement
{
    public CQFLayoutPlacement(CustomMapDataDef source, Rot4 rotation)
    {
        Source = source;
        Rotation = rotation;
        Data = !source.rot.IsValid || rotation == source.rot ? source.Copy("_Layout") : source.GetRotated(rotation).Copy("_Layout");
        List<IntVec3> cells = Data.GetAllPosition();
        cells.AddRange(Data.regions.SelectMany(r => r.cells));
        RotationDirection turn = source.rot.IsValid ? Rot4.GetRelativeRotation(source.rot, rotation) : RotationDirection.None;
        foreach (IntVec3 corner in new CellRect(0, 0, Math.Max(1, source.size.x), Math.Max(1, source.size.z)).Corners)
        {
            cells.Add(turn == RotationDirection.Clockwise ? new IntVec3(corner.z, 0, -corner.x)
                : turn == RotationDirection.Counterclockwise ? new IntVec3(-corner.z, 0, corner.x)
                : turn == RotationDirection.Opposite ? new IntVec3(-corner.x, 0, -corner.z) : corner);
        }
        foreach (ThingData thing in Data.thingDatas)
        {
            IEnumerable<IntVec3> positions = thing.allPositions.Concat(thing.allRect.SelectMany(r => r.Cells));
            if (!positions.Any()) positions = new[] { thing.position };
            foreach (IntVec3 position in positions)
            {
                List<IntVec3> occupied = GenAdj.OccupiedRect(position, thing.rotation, thing.def.size).Cells.ToList();
                cells.AddRange(occupied); ObjectCells.UnionWith(occupied);
                if (thing.def.passability == Traversability.Impassable) ImpassableCells.UnionWith(occupied);
            }
        }
        foreach (CustomThingData thing in Data.customThings)
        {
            List<IntVec3> occupied = GenAdj.OccupiedRect(thing.position, thing.rotation, thing.def.size).Cells.ToList();
            cells.AddRange(occupied); ObjectCells.UnionWith(occupied);
            if (thing.def.passability == Traversability.Impassable) ImpassableCells.UnionWith(occupied);
        }
        LocalBounds = CellRect.FromLimits(cells.Min(c => c.x), cells.Min(c => c.z), cells.Max(c => c.x), cells.Max(c => c.z));
    }

    public CustomMapDataDef Source { get; }
    public CustomMapDataDef Data { get; }
    public Rot4 Rotation { get; }
    public CellRect LocalBounds { get; }
    public CellRect Bounds => new CellRect(LocalBounds.minX + Origin.x, LocalBounds.minZ + Origin.z, LocalBounds.Width, LocalBounds.Height);
    public IntVec3 Center => Bounds.CenterCell;
    public HashSet<IntVec3> ObjectCells { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> ImpassableCells { get; } = new HashSet<IntVec3>();

    public void MoveCenter(IntVec3 center)
    {
        Origin = center - LocalBounds.CenterCell;
    }

    public IEnumerable<CQFMapRegionConnection> Entrances()
    {
        foreach (CQFMapRegionConnection connection in Data.regions.SelectMany(r => r.connections))
        {
            CQFMapRegionConnection result = connection.Copy(); result.cell += Origin; yield return result;
        }
        foreach (CustomThingData_ZoneCore core in Data.zoneCores.OfType<CustomThingData_ZoneCore>())
            if (core.coreRotation.IsValid && !core.isCenter)
                yield return new CQFMapRegionConnection { cell = core.position + Origin, direction = core.coreRotation, width = 1 };
        foreach (IntVec3 cell in Data.enterSpots)
            if (Data.enterDirection.IsValid) yield return new CQFMapRegionConnection { cell = cell + Origin, direction = Data.enterDirection, width = 1 };
    }

    public IntVec3 Origin;
    public string Role = "";
    public string ReferenceId = "";
}
