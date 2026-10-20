using UnityEngine;
using Verse;
using RimWorld;

namespace QuestEditor_Library;

public class DomainWorker : IExposable, IDisposable
{
    public MapComponent_Domain Domain { get; internal set; } = null!;
    public Map map => Domain.map;
    public bool Active => Domain.Active;
    public bool Ready => Domain.Ready;
    public bool Available => Domain.Available;
    public Pawn? Caster => Domain.Caster;
    public bool ArrivalLocked => Domain.ArrivalLocked;
    public virtual bool SuspendLifecycle => false;
    public virtual bool HandlesEntryView => false;
    public virtual bool SupportsContests => false;
    public virtual bool RendersSurfaceTerrain => false;
    public int LastRenderedFrame { get => Domain.LastRenderedFrame; set => Domain.LastRenderedFrame = value; }
    public int LastTerrainRenderedFrame { get => Domain.LastTerrainRenderedFrame; set => Domain.LastTerrainRenderedFrame = value; }
    protected Pawn? caster => Domain.Caster;
    protected IReadOnlyList<DomainParticipant> participants => Domain.Participants;
    protected bool arrivalLocked => Domain.ArrivalLocked;

    public void RequestReturn(string reason = "manual recall") => Domain.RequestReturn(reason);
    public void Collapse(string reason) => Domain.Collapse(reason);
    public void BeginArrivalTransition() => Domain.BeginArrivalTransition();
    public void EndArrivalTransition() => Domain.EndArrivalTransition();
    public bool OwnsCell(IntVec3 cell) => Domain.OwnsCell(cell);
    public virtual DomainStructureBlueprint CaptureStructure(Building building) => new(building);
    public virtual Material? GetTerrainMaterial(TerrainDef terrain)
        => terrain.dontRender ? null : map.terrainGrid.GetMaterial(terrain, false, null);
    public IntVec3 FindFreeCell(Map destination, IntVec3 target, bool excludePawns) => Domain.FindFreeCell(destination, target, excludePawns);
    public virtual void Entering() { }
    public virtual void ContestStarted() { }
    public virtual void PreparingContest() { }
    public virtual void TerritoryChanged() { }
    public virtual void Entered(IReadOnlyList<Pawn> entrants) { }
    public virtual void BeforeReturn(bool visible) { }
    public virtual bool PrepareReturn(bool visible)
    {
        BeforeReturn(visible);
        return true;
    }
    public virtual void PawnLeaving(Pawn pawn) { }
    public virtual void Dissolved() { }
    public virtual void Returned(Map destination, IntVec3 focus, IReadOnlyList<Pawn> pawns) { }
    public virtual void Recycling(Map destination) { }
    public virtual void Collapsed(string reason) { }
    public virtual void CasterDied(Pawn pawn) { }
    public virtual bool CasterIsPresent => caster?.MapHeld == map;
    public virtual void Tick() { }
    public virtual void Update() { }
    public virtual void ManagedUpdate() { }
    public virtual void Draw() { }
    public virtual void OnGUI() { }
    public virtual void FinalizeInit() { }
    public virtual void ExposeData() { }
    public virtual void Dispose() { }
}
