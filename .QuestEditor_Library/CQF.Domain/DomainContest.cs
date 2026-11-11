using UnityEngine;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainContest : IExposable, IDisposable
{
    public DomainContest() { }

    public DomainContest(MapComponent_Domain first, DomainTerrainSnapshot secondTerrain)
    {
        firstTerrain = new DomainTerrainSnapshot(first.map);
        this.secondTerrain = secondTerrain;
    }

    public DomainTerritoryPartition Partition => partition ?? throw new InvalidOperationException("[CQF] Contest territory has not been initialized.");
    public float RecoilAge => damageTick < 0 ? 100f : Math.Max(0, GenTicks.TicksGame - damageTick) / 60f;
    public float Recoil => recoil * Mathf.Exp(-RecoilAge * 3f);

    public void RecordDamage(MapComponent_Domain side, float amount)
    {
        if (amount <= 0f) return;
        damageTick = GenTicks.TicksGame;
        recoil = Mathf.Clamp01(Mathf.Sqrt(amount / side.MaximumHealth) * 6f + 0.2f)
            * (ReferenceEquals(side, side.Battlefield) ? 1f : -1f);
    }

    public void Initialize(MapComponent_Domain first)
    {
#if RIMWORLD_1_7
        sourceVisualTick = GenTicks.TicksGame;
#endif
        MapComponent_Domain second = first.Challenger ?? throw new InvalidOperationException("[CQF] Contest has no challenger.");
        bool reverse = first.Caster!.Position.x > second.Caster!.Position.x;
        partition = new DomainTerritoryPartition(first.map.Size.x, first.map.Size.z,
            DomainTerritoryPartition.CalculateFraction(first.Health, second.Health), reverse, first.Caster.Position, second.Caster.Position);
        foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
        RefreshLayouts(first);
        first.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
    }

    public MapComponent_Domain? OwnerAt(MapComponent_Domain first, IntVec3 cell)
    {
        if (first.ActiveDomainCount == 1)
            for (int index = 0; index < first.InstanceCount; index++)
            {
                MapComponent_Domain survivor = first.InstanceAt(index);
                if (survivor.Active) return survivor;
            }
        int owner = Partition.OwnerIndex(cell);
        if (owner < 0 || owner >= first.InstanceCount) return null;
        MapComponent_Domain side = first.InstanceAt(owner);
        return side.Active ? side : null;
    }

    public bool CoversBoundaryCell(IntVec3 cell)
        => Partition.IsBoundaryCell(cell);

    public TerrainDef TerrainAt(MapComponent_Domain side, IntVec3 cell)
        => (allTerrains != null ? allTerrains[side.InstanceIndex]
            : ReferenceEquals(side, side.Battlefield) ? firstTerrain! : secondTerrain!).At(cell, side.map.Size);

    public void Tick(MapComponent_Domain first)
    {
        if (partition == null || first.Challenger == null) return;
        SettleHealth(first);
        if (Partition.IsMultilateral) { TickMultiple(first); return; }
        Partition.PruneProtections(GenTicks.TicksGame, first.Ready, first.Challenger.Ready);
        Partition.PruneClaims(first.Ready, first.Challenger.Ready);
        Partition.ErodeClaims(GenTicks.TicksGame, first.Ready && first.Challenger.Ready
            && !first.ArrivalLocked && !first.Challenger.ArrivalLocked
            && first.Worker?.SuspendLifecycle != true && first.Challenger.Worker?.SuspendLifecycle != true,
            first.Definition.contestClaimErosionTicks, first.Challenger.Definition.contestClaimErosionTicks);
        TickOccupation(first);
        float firstHealth = first.Active ? first.Health : 0f;
        float secondHealth = first.Challenger.Active ? first.Challenger.Health : 0f;
        int oldColumns = Partition.FirstColumns;
        int revision = Partition.Revision;
        float old = Partition.Advance(firstHealth, secondHealth, first.map.Size.x / (float)first.Definition.contestBoundaryTicks);
        if (GenTicks.TicksGame % 60 == 0) RefreshLayouts(first);
        if (revision == Partition.Revision) return;
        dirtySections.Clear();
        if (Partition.HasOverrides || Partition.ChangedCells.Count > 0)
        {
            foreach (IntVec3 cell in Partition.ChangedCells) ApplyCell(first, cell);
        }
        else
            for (int column = Math.Min(oldColumns, Partition.FirstColumns); column < Math.Max(oldColumns, Partition.FirstColumns); column++)
            {
                int x = Partition.Reversed ? first.map.Size.x - 1 - column : column;
                for (int z = 0; z < first.map.Size.z; z++) ApplyCell(first, new IntVec3(x, 0, z));
            }
        MarkBounds(first, new Rect(Math.Min(old, Partition.BoundaryX) - 1f, 0f, Math.Abs(old - Partition.BoundaryX) + 2f, first.map.Size.z));
        foreach (Rect bounds in Partition.DirtyBounds) MarkBounds(first, bounds);
        foreach (Section section in dirtySections)
            first.map.mapDrawer.MapMeshDirty(new IntVec3(section.CellRect.minX, 0, section.CellRect.minZ), MapMeshFlagDefOf.Terrain);
        if (oldColumns != Partition.FirstColumns || Partition.ChangedCells.Count > 0)
        {
            RefreshLayouts(first);
            first.Worker?.TerritoryChanged();
            first.Challenger.Worker?.TerritoryChanged();
        }
    }

    public void RefreshLayouts(MapComponent_Domain first)
    {
        foreach (MapComponent_Domain side in first.Instances) side.Layout?.ParkOutside();
        foreach (MapComponent_Domain side in first.Instances) side.Layout?.RevealOwned();
    }

    public void ResolveSurvivor(MapComponent_Domain first)
    {
        if (Partition.IsMultilateral)
        {
            foreach (DomainOccupation timer in occupations!) timer.Advance(GenTicks.TicksGame, false);
            List<MapComponent_Domain> sides = first.Instances.ToList();
            Partition.Prune(GenTicks.TicksGame, sides.Select(side => side.Active).ToList());
            Partition.Advance(sides.Select(side => side.Active ? side.Health : 0f).ToList(), 1f, true);
            foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
            RefreshLayouts(first);
            first.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
            return;
        }
        firstOccupation?.Advance(GenTicks.TicksGame, false);
        secondOccupation?.Advance(GenTicks.TicksGame, false);
        Partition.PruneProtections(GenTicks.TicksGame, first.Active, first.Challenger?.Active == true);
        Partition.PruneClaims(first.Active, first.Challenger?.Active == true);
        Partition.Advance(first.Active ? first.Health : 0f, first.Challenger?.Active == true ? first.Challenger.Health : 0f, first.map.Size.x);
        foreach (IntVec3 cell in first.map.AllCells) ApplyCell(first, cell);
        RefreshLayouts(first);
        first.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
    }

    public void Draw(MapComponent_Domain first)
    {
        if (partition == null || !first.HasActiveDomain) return;
        if (Partition.IsMultilateral)
        {
            multiOverlay ??= new DomainMultiContestOverlay(first);
            multiOverlay.Draw(first, Partition);
            return;
        }
        overlay ??= new DomainContestOverlay(first);
        overlay.Draw(first, Partition);
    }

    public float DrawSource(MapComponent_Domain domain, Building_Domain anchor)
    {
#if RIMWORLD_1_7
        if (domain.InstanceCount > 2) return DrawSourceGroup(domain, anchor);
        MapComponent_Domain first = domain.Battlefield;
        MapComponent_Domain? second = first.Challenger;
        if (sourceVisualError || !first.Active || second?.Active != true
            || first.Anchor?.Spawned != true || second.Anchor?.Spawned != true || first.Anchor.Map != second.Anchor.Map) return 0f;
        float firstRadius = first.Definition.OpeningRadius;
        float secondRadius = second.Definition.OpeningRadius;
        if (first.Anchor.Position.DistanceToSquared(second.Anchor.Position) >= (firstRadius + secondRadius) * (firstRadius + secondRadius)) return 0f;
        if (sourceVisualTick < 0) sourceVisualTick = GenTicks.TicksGame;
        float age = Math.Max(0, GenTicks.TicksGame - sourceVisualTick) / 60f;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.4f));
        if (anchor != first.Anchor || blend <= 0f) return blend;
        try
        {
            sourceVisuals ??= new DomainFusionVisuals(first.Definition, second.Definition);
            sourceVisuals.Draw(first.Anchor.Position.ToVector3Shifted(), firstRadius, second.Anchor.Position.ToVector3Shifted(), secondRadius,
                GenTicks.TicksGame / 60f, age, blend, DomainTerritoryPartition.CalculateFraction(first.Health, second.Health));
            return blend;
        }
        catch (Exception error)
        {
            sourceVisualError = true;
            sourceVisuals?.Dispose();
            sourceVisuals = null;
            Log.Error("[CQF] Contest source-map fusion visuals failed: " + error);
        }
#endif
        return 0f;
    }

    public DomainFusionState DomeStateFor(MapComponent_Domain domain, float blend)
    {
        MapComponent_Domain first = domain.Battlefield;
        MapComponent_Domain? rival = first.InstanceCount > 2 ? first.Instances.Where(side => side.Active && side != domain
            && side.Anchor?.Spawned == true && side.Anchor.Map == domain.Anchor?.Map)
            .OrderBy(side => side.Anchor!.Position.DistanceToSquared(domain.Anchor!.Position)).FirstOrDefault()
            : ReferenceEquals(first, domain) ? first.Challenger : first;
        if (rival == null) return default;
        float radius = domain.Definition.OpeningRadius;
        float rivalRadius = rival.Definition.OpeningRadius;
        bool second = domain.InstanceIndex > rival.InstanceIndex;
        float bias = (0.5f - DomainTerritoryPartition.CalculateFraction(second ? rival.Health : domain.Health,
            second ? domain.Health : rival.Health)) * Math.Min(radius, rivalRadius) * 3f;
        return new DomainFusionState(domain.Anchor!.Position.ToVector3Shifted(), radius, rival.Anchor!.Position.ToVector3Shifted(), rivalRadius,
            second, GenTicks.TicksGame / 60f + 6f, blend, bias);
    }

    public void Restore(MapComponent_Domain first) => firstTerrain?.Restore(first.map);

    public void ExposeData()
    {
        Scribe_Collections.Look(ref allTerrains, "allTerrains", LookMode.Deep);
        Scribe_Collections.Look(ref occupations, "occupations", LookMode.Deep);
        if (allTerrains == null)
        {
            Scribe_Deep.Look(ref firstTerrain, "firstTerrain");
            Scribe_Deep.Look(ref secondTerrain, "secondTerrain");
        }
        else if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (allTerrains.Count < 3 || allTerrains.Count > MapComponent_Domain.MaximumDomains
                || occupations?.Count != allTerrains.Count)
                throw new InvalidOperationException("[CQF] Invalid saved multiple-domain terrain or occupation state.");
            firstTerrain = allTerrains[0]; secondTerrain = allTerrains[1];
        }
        Scribe_Deep.Look(ref partition, "partition");
        Scribe_Values.Look(ref damageTick, "damageTick", -1);
        Scribe_Values.Look(ref recoil, "recoil");
        Scribe_Values.Look(ref settlementTick, "settlementTick", -1);
        Scribe_Deep.Look(ref firstOccupation, "firstOccupation");
        Scribe_Deep.Look(ref secondOccupation, "secondOccupation");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            firstOccupation ??= new();
            secondOccupation ??= new();
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (firstTerrain == null || secondTerrain == null || partition == null))
            throw new InvalidOperationException("[CQF] Contest save is missing its terrain or partition.");
    }

    public void Dispose()
    {
#if RIMWORLD_1_7
        sourceVisuals?.Dispose();
        sourceVisuals = null;
        foreach (DomainFusionVisuals visuals in sourcePairs.Values) visuals.Dispose();
        sourcePairs.Clear();
#endif
        overlay?.Dispose();
        overlay = null;
        multiOverlay?.Dispose();
        multiOverlay = null;
    }

    private void ApplyCell(MapComponent_Domain first, IntVec3 cell)
    {
        MapComponent_Domain? side = OwnerAt(first, cell);
        if (side == null) return;
        TerrainDef terrain = TerrainAt(side, cell);
        if (first.map.terrainGrid.TerrainAt(cell) != terrain) first.map.terrainGrid.SetTerrain(cell, terrain);
    }

    private void SettleHealth(MapComponent_Domain first)
    {
        int tick = GenTicks.TicksGame;
        if (settlementTick == tick) return;
        settlementTick = tick;
        List<MapComponent_Domain> sides = first.Instances.Where(side => side.Active).ToList();
        if (sides.Count < 2 || sides.Any(side => !side.Ready || side.ArrivalLocked || side.Worker?.SuspendLifecycle == true)) return;
        float multiplier = HealthLossMultiplier(sides.Count);
        foreach (MapComponent_Domain side in sides)
            side.SpendHealth(side.MaximumHealth * side.Definition.contestHealthLossPerSecond * multiplier / 60f, false);
    }

    private void MarkBounds(MapComponent_Domain first, Rect bounds)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt(bounds.xMin), 0, first.map.Size.x - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(bounds.xMax), 0, first.map.Size.x - 1);
        int minZ = Mathf.Clamp(Mathf.FloorToInt(bounds.yMin), 0, first.map.Size.z - 1);
        int maxZ = Mathf.Clamp(Mathf.CeilToInt(bounds.yMax), 0, first.map.Size.z - 1);
        for (int x = minX; x <= maxX;)
        {
            CellRect rect = default;
            for (int z = minZ; z <= maxZ;)
            {
                Section section = first.map.mapDrawer.SectionAt(new IntVec3(x, 0, z));
                dirtySections.Add(section);
                rect = section.CellRect;
                z = rect.maxZ + 1;
            }
            x = rect.maxX + 1;
        }
    }

    private DomainTerrainSnapshot? firstTerrain;
    private DomainTerrainSnapshot? secondTerrain;
    private DomainTerritoryPartition? partition;
    private DomainContestOverlay? overlay;
    private readonly HashSet<Section> dirtySections = new();
    private int damageTick = -1;
    private float recoil;
    private int settlementTick = -1;
#if RIMWORLD_1_7
    private DomainFusionVisuals? sourceVisuals;
    private int sourceVisualTick = -1;
    private bool sourceVisualError;
#endif
}
