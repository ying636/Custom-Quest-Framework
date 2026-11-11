using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainContest
{
    public static float HealthLossMultiplier(int activeDomains)
    {
        if (activeDomains < 0 || activeDomains > MapComponent_Domain.MaximumDomains) throw new ArgumentOutOfRangeException(nameof(activeDomains));
        return Math.Max(0, activeDomains - 1);
    }

    internal DomainOccupation OccupationFor(int side)
        => occupations != null ? occupations[side] : (side == 0 ? firstOccupation : secondOccupation)!;

    internal void InitializeScene(MapComponent_Domain first, IReadOnlyList<DomainTerrainSnapshot> terrains,
        IReadOnlyList<IntVec3> centers)
    {
        List<MapComponent_Domain> sides = first.Instances.ToList();
        if (terrains.Count != sides.Count || centers.Count != sides.Count || sides.Count < 2)
            throw new InvalidOperationException("[CQF] Inconsistent multiple-domain scene.");
        firstTerrain = terrains[0]; secondTerrain = terrains[1];
        if (sides.Count == 2)
        {
            partition = new DomainTerritoryPartition(first.map.Size.x, first.map.Size.z,
                DomainTerritoryPartition.CalculateFraction(sides[0].Health, sides[1].Health), centers[0].x > centers[1].x, centers[0], centers[1]);
        }
        else
        {
            allTerrains = terrains.ToList();
            occupations = sides.Select(_ => new DomainOccupation()).ToList();
            partition = new DomainTerritoryPartition(first.map.Size.x, first.map.Size.z, centers,
                sides.Select(side => side.Active ? side.Health : 0f).ToList());
        }
        foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
        RefreshLayouts(first);
        first.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
    }

    internal void Inherit(MapComponent_Domain first, DomainContest previous, IReadOnlyList<int> ownerMap, IntVec3 offset)
    {
        if (!Partition.IsMultilateral) return;
        settlementTick = Math.Max(settlementTick, previous.settlementTick);
        Partition.Inherit(previous.Partition, ownerMap, offset);
        for (int side = 0; side < ownerMap.Count; side++) occupations![ownerMap[side]] = previous.OccupationFor(side);
        Partition.Advance(first.Instances.Select(side => side.Active ? side.Health : 0f).ToList(), 1f, true);
        foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
        RefreshLayouts(first);
    }

    internal void AppendSide(MapComponent_Domain first, DomainMapTemplate template)
    {
        DomainTerritoryPartition old = Partition;
        List<MapComponent_Domain> sides = first.Instances.ToList();
        List<DomainTerrainSnapshot> terrains = allTerrains?.ToList() ?? new() { firstTerrain!, secondTerrain! };
        List<DomainOccupation> timers = occupations?.ToList() ?? new() { firstOccupation!, secondOccupation! };
        terrains.Add(template.Terrain);
        List<IntVec3> centers = old.OpeningCenters.ToList();
        centers.Add(sides[sides.Count - 1].Caster!.Position);
        overlay?.Dispose(); overlay = null;
        multiOverlay?.Dispose(); multiOverlay = null;
        InitializeScene(first, terrains, centers);
        Partition.Inherit(old, Enumerable.Range(0, sides.Count - 1).ToList(), IntVec3.Zero);
        for (int index = 0; index < timers.Count; index++) occupations![index] = timers[index];
        Partition.Advance(sides.Select(side => side.Active ? side.Health : 0f).ToList(), 1f, true);
        foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
        RefreshLayouts(first);
    }

    private void TickMultiple(MapComponent_Domain first)
    {
        List<MapComponent_Domain> sides = first.Instances.ToList();
        bool ready = sides.Where(side => side.Active).All(side => side.Ready && !side.ArrivalLocked && side.Worker?.SuspendLifecycle != true);
        int revision = Partition.Revision;
        Partition.Prune(GenTicks.TicksGame, sides.Select(side => side.Ready).ToList());
        Partition.ErodeClaims(GenTicks.TicksGame, ready, sides.Select(side => side.Definition.contestClaimErosionTicks).ToList());
        List<IntVec3?> captures = new();
        bool capturing = false;
        for (int index = 0; index < sides.Count; index++)
        {
            MapComponent_Domain side = sides[index];
            bool capture = occupations![index].Advance(GenTicks.TicksGame, ready && first.ActiveDomainCount > 1 && side.Ready && CanOccupy(side));
            captures.Add(capture ? side.Caster!.Position : null);
            capturing |= capture;
        }
        if (capturing)
        {
            int[] gained = Partition.Capture(captures);
            for (int index = 0; index < gained.Length; index++) if (gained[index] > 0) sides[index].RestoreHealth(300f);
        }
        int duration = sides.Where(side => side.Active).Select(side => side.Definition.contestBoundaryTicks).DefaultIfEmpty(120).Max();
        Partition.Advance(sides.Select(side => side.Active ? side.Health : 0f).ToList(), 1f / duration);
        if (GenTicks.TicksGame % 60 == 0) RefreshLayouts(first);
        if (revision == Partition.Revision) return;
        dirtySections.Clear();
        foreach (IntVec3 cell in Partition.ChangedCells) ApplyCell(first, cell);
        foreach (UnityEngine.Rect bounds in Partition.DirtyBounds) MarkBounds(first, bounds);
        foreach (Section section in dirtySections)
            first.map.mapDrawer.MapMeshDirty(new IntVec3(section.CellRect.minX, 0, section.CellRect.minZ), MapMeshFlagDefOf.Terrain);
        if (Partition.ChangedCells.Count == 0) return;
        RefreshLayouts(first);
        foreach (MapComponent_Domain side in sides.Where(side => side.Active)) side.Worker?.TerritoryChanged();
    }

    private List<DomainTerrainSnapshot>? allTerrains;
    private List<DomainOccupation>? occupations;
    private DomainMultiContestOverlay? multiOverlay;
}
