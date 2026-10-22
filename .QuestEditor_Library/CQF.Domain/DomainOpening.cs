using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class DomainOpening : Thing
{
    public abstract DomainDef Definition { get; }
    public abstract Pawn? Caster { get; }
    public abstract bool Active { get; }
    public virtual bool SupportsFusion => false;
    public virtual float Progress => startTick < 0 ? 1f : IsClosing
        ? ClosingStart + (1f - ClosingStart) * Mathf.Clamp01((GenTicks.TicksGame - closingTick) / (float)ClosingTicks)
        : ClosingStart * Mathf.Clamp01((GenTicks.TicksGame - startTick) / (float)OpeningTicks);
    public virtual float Radius => Definition.OpeningRadius;
    public virtual IReadOnlyList<Pawn> Entrants => entrants;
    public virtual int OpeningTicks => Definition.visuals?.openingTicks ?? 360;
    public virtual int ClosingTicks => Definition.visuals?.closingTicks ?? 120;
    public bool IsClosing => closingTick >= 0;
    public bool IsCounterOpening => counterOpening;
    public bool ReadyToClose => startTick >= 0 && GenTicks.TicksGame - startTick >= OpeningTicks;
    protected bool ReadyToTransfer => IsCounterOpening ? IsClosing : Progress >= 1f;
    protected bool CounterOpeningValid => !IsCounterOpening || Map.GetComponent<MapComponent_Domain>().CanAcceptChallenger;
    protected virtual float ClosingStart => Definition.visuals?.foldStart ?? 0.7f;
    public DomainOpening_Fusion? Fusion { get => fusion; internal set => fusion = value; }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_Domain>().RegisterOpening(this);
        runtime = CQFDomainRuntime.Instance;
        runtime.RegisterOpening(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseOpeningLocks();
        if (startTick >= 0 && Caster is Pawn caster && Definition.openingAbility != null && caster.CurJob?.ability?.def == Definition.openingAbility)
            caster.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
        Map.GetComponent<MapComponent_Domain>().UnregisterOpening(this);
        runtime?.UnregisterOpening(this);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref fusion, "fusion");
        Scribe_Values.Look(ref startTick, "phaseStartTick", -1);
        Scribe_Values.Look(ref closingTick, "closingTick", -1);
        Scribe_Values.Look(ref counterOpening, "counterOpening");
        Scribe_Values.Look(ref locksReleased, "openingLocksReleased");
        Scribe_Collections.Look(ref entrants, "closingEntrants", LookMode.Reference);
        Scribe_Collections.Look(ref lockedPawns, "openingLockedPawns", LookMode.Reference);
        Scribe_Collections.Look(ref originalStunEnds, "openingOriginalStunEnds", LookMode.Value);
        Scribe_Collections.Look(ref appliedStunEnds, "openingAppliedStunEnds", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) entrantSet.UnionWith(entrants);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (lockedPawns.Count != originalStunEnds.Count || lockedPawns.Count != appliedStunEnds.Count))
            throw new InvalidOperationException("[CQF] Domain opening has invalid saved lock records.");
    }

    public virtual void ReleaseForFusion() => ReleaseOpeningLocks();
    public virtual void ResumeAfterFusion() { }

    public List<Pawn> CaptureEntrants()
        => (IsClosing ? Entrants : Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.Position.DistanceToSquared(Position) <= Radius * Radius))
            .Where(pawn => pawn != null && pawn.Spawned && !pawn.Dead && pawn.Map == Map).Distinct().ToList();

    public void BeginClosing()
    {
        if (IsClosing || !Active) return;
        closingTick = GenTicks.TicksGame;
        entrants.Clear();
        entrantSet.Clear();
        CaptureClosingPawns();
    }

    internal int? OriginalStunEnd(Pawn pawn)
    {
        if (locksReleased) return null;
        int index = lockedPawns.IndexOf(pawn);
        return index < 0 ? null : originalStunEnds[index];
    }

    public abstract void CompleteOpening();
    public abstract void CompleteTransfer();

    protected void InitializeOpening()
    {
        startTick = GenTicks.TicksGame;
        counterOpening = Map.GetComponent<MapComponent_Domain>().HasActiveDomain;
        if (Caster == null) throw new InvalidOperationException("[CQF] Domain opening has no caster.");
        entrants.Add(Caster);
        entrantSet.Add(Caster);
        LockPawn(Caster);
        if (Find.CurrentMap == Map && !Find.TickManager.Paused
            && Map.mapPawns.FreeColonistsSpawned.Any(pawn => pawn.Position.DistanceToSquared(Position) <= Radius * Radius))
            Find.TickManager.CurTimeSpeed = Verse.TimeSpeed.Normal;
    }

    protected virtual void ClosingStarted(IReadOnlyList<Pawn> pawns) { }

    protected void BindEntryTransition(MapComponent_Domain domain)
    {
        DomainTransition? transition = runtime!.Transition;
        if (IsCounterOpening) transition?.BindContest(domain.Battlefield, domain);
        else transition?.Bind(domain);
    }

    protected override void Tick()
    {
        base.Tick();
        if (startTick < 0 || !Active || locksReleased) return;
        if (IsClosing) CaptureClosingPawns();
        for (int index = 0; index < lockedPawns.Count; index++)
        {
            Pawn pawn = lockedPawns[index];
            if (pawn == null || pawn.Dead || pawn.MapHeld != Map) continue;
            appliedStunEnds[index] = GenTicks.TicksGame + 2;
            pawn.pather?.StopDead();
            pawn.stances.stunner.StunFor(2, Caster, false, false, true);
        }
        if (!IsClosing && ReadyToClose && Fusion?.Active != true && !runtime!.HasPendingCounter(this)) BeginClosing();
    }

    protected void ReleaseOpeningLocks()
    {
        if (locksReleased) return;
        locksReleased = true;
        for (int index = 0; index < lockedPawns.Count; index++)
        {
            Pawn pawn = lockedPawns[index];
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != Map || runtime?.FindOpeningStunEnd(pawn, this) != null) continue;
            int remaining = Math.Max(0, originalStunEnds[index] - GenTicks.TicksGame);
            int applied = Math.Max(0, appliedStunEnds[index] - GenTicks.TicksGame);
            if (pawn.stances.stunner.StunTicksLeft > Math.Max(remaining, applied)) continue;
            pawn.stances.stunner.StopStun();
            if (remaining > 0) pawn.stances.stunner.StunFor(remaining, null, false, false, true);
        }
    }

    private void LockPawn(Pawn pawn)
    {
        if (lockedPawns.Contains(pawn)) return;
        int original = runtime!.FindOpeningStunEnd(pawn, this) ?? GenTicks.TicksGame + pawn.stances.stunner.StunTicksLeft;
        lockedPawns.Add(pawn);
        originalStunEnds.Add(original);
        appliedStunEnds.Add(GenTicks.TicksGame + 2);
        pawn.pather?.StopDead();
        pawn.stances.stunner.StunFor(2, Caster, false, false, true);
    }

    private void CaptureClosingPawns()
    {
        List<Pawn>? additions = null;
        foreach (Pawn pawn in Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Dead || entrantSet.Contains(pawn) || pawn.Position.DistanceToSquared(Position) > Radius * Radius
                || pawn != Caster && runtime!.IsOpeningCaster(pawn)) continue;
            additions ??= new List<Pawn>();
            additions.Add(pawn);
            entrantSet.Add(pawn);
        }
        if (additions == null) return;
        entrants.AddRange(additions);
        foreach (Pawn pawn in additions) LockPawn(pawn);
        ClosingStarted(additions);
    }

    private DomainOpening_Fusion? fusion;
    private CQFDomainRuntime? runtime;
    private int startTick = -1;
    private int closingTick = -1;
    private bool counterOpening;
    private bool locksReleased;
    private List<Pawn> entrants = new();
    private List<Pawn> lockedPawns = new();
    private List<int> originalStunEnds = new();
    private List<int> appliedStunEnds = new();
    private readonly HashSet<Pawn> entrantSet = new();
}
