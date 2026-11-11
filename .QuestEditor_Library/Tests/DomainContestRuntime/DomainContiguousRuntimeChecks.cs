using HarmonyLib;
using QuestEditor_Library;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public static class DomainContiguousRuntimeChecks
{
    public static void Validate(MapComponent_Domain field, Action<bool, string> check)
    {
        if (field.Contest?.Partition.IsMultilateral != true) return;
        DomainMultiTerritory main = (DomainMultiTerritory)AccessTools.Field(typeof(DomainTerritoryPartition), "multi").GetValue(field.Contest.Partition);
        foreach (MapComponent_Domain side in field.Instances.Where(side => side.Active))
        {
            int owner = side.InstanceIndex;
            HashSet<IntVec3> cells = field.map.AllCells.Where(cell => main.BaseOwner(cell.x + 0.5f, cell.z + 0.5f) == owner).ToHashSet();
            HashSet<IntVec3> reached = new();
            Queue<IntVec3> pending = new();
            if (cells.Count > 0) pending.Enqueue(cells.First());
            while (pending.Count > 0)
            {
                IntVec3 cell = pending.Dequeue();
                if (!reached.Add(cell)) continue;
                foreach (IntVec3 direction in GenAdj.CardinalDirections)
                    if (cells.Contains(cell + direction) && !reached.Contains(cell + direction)) pending.Enqueue(cell + direction);
            }
            check(cells.Count > 0 && reached.Count == cells.Count, "main territory is one connected block: " + side.Definition.defName);
            int width = cells.Max(cell => cell.x) - cells.Min(cell => cell.x) + 1;
            int height = cells.Max(cell => cell.z) - cells.Min(cell => cell.z) + 1;
            check(cells.Count < 32 || cells.Count >= width * height * 0.3d && Math.Max(width, height) <= Math.Min(width, height) * 5,
                "main territory remains compact: " + side.Definition.defName);
        }
    }
}
