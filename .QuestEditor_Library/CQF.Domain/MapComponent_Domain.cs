using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class MapComponent_Domain : MapComponent
{
    public MapComponent_Domain(Map map) : this(map, false) { }

    public MapComponent_Domain(Map map, bool independentInstance) : base(map)
    {
        this.independentInstance = independentInstance;
    }

    public DomainDef Definition => definition ?? throw new InvalidOperationException("[CQF] Map has no domain definition.");
    public DomainWorker? Worker => worker;
    public bool Active => active;
    public bool Ready => active && !closing;
    public bool Available => !active && !closing && !trapped && participants.Count == 0 && (challenger == null || challenger.Available);
    public bool ArrivalLocked => arrivalLocked;
    public Pawn? Caster => caster;
    public Map? SourceMap => sourceMap;
    public IntVec3 SourceOrigin => sourceOrigin;
    public IReadOnlyList<DomainParticipant> Participants => participants;
    public float Health => health;
    public float MaximumHealth => Definition.maximumHealth;
    public float HealthFraction => Mathf.Clamp01(health / MaximumHealth);
    public Building_Domain? Anchor => anchor;
    public DomainLayout? Layout => layout;
    public int LastRenderedFrame { get; set; } = -100;
    public int LastTerrainRenderedFrame { get; set; } = -100;

    public T? GetWorker<T>() where T : DomainWorker => worker as T;

    public bool ProtectsFromOpening(Pawn pawn)
    {
        if (pawn.Dead || pawn.Downed) return false;
        if (active && (entering || arrivalLocked) && caster == pawn) return true;
        if (challenger?.ProtectsFromOpening(pawn) == true) return true;
        foreach (DomainOpening opening in openings)
            if (!opening.Destroyed && opening.Active && opening.Caster == pawn) return true;
        return false;
    }

    public void RegisterOpening(DomainOpening opening)
    {
        openings.Add(opening);
        if (opening.Caster != null) Comp_DomainProtection.Ensure(opening.Caster);
    }

    public void UnregisterOpening(DomainOpening opening) => openings.Remove(opening);

    public void Configure(DomainDef def)
    {
        if (definition == def && worker != null) return;
        if (!Available) throw new InvalidOperationException("[CQF] Cannot reconfigure an occupied domain.");
        worker?.Dispose();
        definition = def;
        worker = Activator.CreateInstance(def.workerClass) as DomainWorker
            ?? throw new InvalidOperationException("[CQF] Could not create domain worker: " + def.workerClass);
        worker.Domain = this;
    }

    public void Enter(Pawn pawn, IEnumerable<Pawn> targets)
    {
        if (!Available || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map == map)
            throw new InvalidOperationException("[CQF] Invalid domain entry state.");
        Comp_DomainProtection.Ensure(pawn);
        List<Pawn> entrants = new() { pawn };
        entrants.AddRange(targets.Where(target => target != pawn && !target.Dead && target.Spawned && target.Map == pawn.Map).Distinct());
        sourceMap = pawn.Map;
        sourceOrigin = pawn.Position;
        caster = pawn;
        active = true;
        closing = false;
        collapsed = false;
        returnErrorReported = false;
        health = MaximumHealth;
        entering = true;
        try
        {
            layout = new DomainLayout(this, CQFDomainRuntime.Instance.GetMapTemplate(Definition, sourceMap), true);
            layout.RevealOwned();
            worker!.Entering();
            if (Definition.protection != null)
            {
                Hediff_DomainProtection protection = pawn.health.AddHediff(Definition.protection) as Hediff_DomainProtection
                    ?? throw new InvalidOperationException("[CQF] Domain protection must derive from Hediff_DomainProtection.");
                protection.Initialize(this);
            }
            foreach (Pawn entrant in entrants)
            {
                DomainParticipant entry = new()
                {
                    pawn = entrant,
                    originalPosition = entrant.Position,
                    originalLord = entrant.GetLord(),
                    drafted = entrant.Drafted
                };
                participants.Add(entry);
                IntVec3 cell = FindFreeCell(map, map.Center + (entry.originalPosition - sourceOrigin), true);
                entry.originalLord?.RemovePawn(entrant);
                entrant.DeSpawn();
                GenSpawn.Spawn(entrant, cell, map);
                if (entrant.drafter != null) entrant.drafter.Drafted = entry.drafted;
            }
            worker.Entered(entrants);
            CreateAnchor();
            CQFDomainRuntime.Instance.NotifyDomainEntered(map);
            if (!worker.HandlesEntryView)
            {
                Current.Game.CurrentMap = map;
                Find.Selector.ClearSelection();
                Find.Selector.Select(pawn);
                Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
            }
        }
        catch (Exception exception)
        {
            Log.Error("[CQF] Domain entry failed; returning transferred participants: " + exception);
            try { ReturnToSource(); }
            catch (Exception rollback) { Log.Error("[CQF] Domain entry rollback failed; retaining the domain: " + rollback); }
            throw;
        }
        finally { entering = false; }
    }

    public void RequestReturn(string reason = "manual recall")
    {
        if (!active || closing) return;
        Log.Message("[CQF] Closing domain " + Definition.defName + ": " + reason);
        closing = true;
    }

    public bool AbsorbDamage(Pawn pawn, ref DamageInfo damage)
    {
        if (!active || caster != pawn || pawn.Dead || health <= 0f || damage.Amount <= 0f) return false;
        if (pawn.MapHeld == map && OwnsCell(pawn.PositionHeld)) damage.SetAmount(damage.Amount * 0.7f);
        float absorbed = Mathf.Min(health, damage.Amount);
        damage.SetAmount(damage.Amount - absorbed);
        DrainHealth(absorbed);
        return damage.Amount <= 0f;
    }

    public void DrainHealth(float amount)
        => SpendHealth(amount, true);

    internal void SpendHealth(float amount, bool notifyDamage)
    {
        if (!active || collapsed || amount <= 0f) return;
        if (float.IsNaN(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (notifyDamage) Contest?.RecordDamage(this, Mathf.Min(health, amount));
        health = Mathf.Max(0f, health - amount);
        anchor?.SynchronizeHitPoints();
        if (health <= 0f) Collapse("domain health exhausted");
    }

    public void Collapse(string reason)
    {
        if (!active || collapsed) return;
        collapsed = true;
        health = 0f;
        anchor?.RemoveNormally();
        anchor = null;
        try { worker!.Collapsed(reason); }
        finally { RequestReturn(reason); }
    }

    public void RestoreHealth(float amount)
    {
        if (!active || collapsed || amount <= 0f) return;
        if (float.IsNaN(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        health = Mathf.Min(MaximumHealth, health + amount);
        anchor?.SynchronizeHitPoints();
    }

    public void ReturnToSource()
    {
        if (!active) return;
        bool contested = Battlefield.HasOtherActiveDomain(this);
        Map? destination = contested ? map : ResolveReturnMap();
        MapComponent_Domain? captive = contested ? null : CaptiveDomain;
        if (destination == null || captive != null && captive.ResolveReturnMap() == null)
        {
            closing = false;
            if (!returnErrorReported)
            {
                Log.Error("[CQF] Domain has no return map; retaining its occupants and map.");
                returnErrorReported = true;
            }
            return;
        }
        bool visible = Find.CurrentMap == map || ((Find.CurrentMap == null || caster?.Dead == true)
            && Time.frameCount - LastRenderedFrame <= 2);
        if (!worker!.PrepareReturn(visible)) return;
        if (contested) HoldInBattlefield(visible);
        else FinishReturn(destination, visible, false);
    }

    public void CompleteReturn()
    {
        try { ReturnToSource(); }
        catch (Exception exception)
        {
            closing = false;
            CQFDomainRuntime.Instance.FinishTransitionFor(map);
            Log.Error("[CQF] Domain return failed; retaining the remaining map: " + exception);
        }
        finally { returnQueued = false; }
    }

    public void BeginArrivalTransition()
    {
        if (!active || arrivalLocked) return;
        arrivalLocked = true;
        foreach (DomainParticipant entry in participants)
        {
            Pawn? pawn = entry.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map) continue;
            entry.transitionStunEnd = Find.TickManager.TicksGame + pawn.stances.stunner.StunTicksLeft;
            pawn.pather?.StopDead();
            pawn.stances.stunner.StunFor(90, caster, false, false, true);
        }
    }

    public void EndArrivalTransition()
    {
        if (!arrivalLocked) return;
        arrivalLocked = false;
        foreach (DomainParticipant entry in participants)
        {
            Pawn? pawn = entry.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map) continue;
            int remaining = Mathf.Max(0, entry.transitionStunEnd - Find.TickManager.TicksGame);
            if (pawn.stances.stunner.StunTicksLeft > Mathf.Max(remaining, 90)) continue;
            pawn.stances.stunner.StopStun();
            if (remaining > 0) pawn.stances.stunner.StunFor(remaining, null, false, false, true);
        }
    }

    public IntVec3 FindFreeCell(Map destination, IntVec3 target, bool excludePawns)
    {
        target.x = Mathf.Clamp(target.x, 0, destination.Size.x - 1);
        target.z = Mathf.Clamp(target.z, 0, destination.Size.z - 1);
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(target, 12f, true))
            if (cell.InBounds(destination) && cell.Standable(destination)
                && (!excludePawns || !cell.GetThingList(destination).Any(thing => thing is Pawn))) return cell;
        foreach (IntVec3 cell in destination.AllCells)
            if (cell.Standable(destination) && (!excludePawns || !cell.GetThingList(destination).Any(thing => thing is Pawn))) return cell;
        throw new InvalidOperationException("[CQF] No safe cell is available for a domain occupant.");
    }

    public override void MapComponentTick()
    {
        challenger?.MapComponentTick();
        contest?.Tick(this);
        if (active && Contest == null && GenTicks.TicksGame % 60 == 0) layout?.RevealOwned();
        if (!active || closing || worker!.SuspendLifecycle) return;
        if (!collapsed && health <= 0f) { Collapse("domain health exhausted"); return; }
        if (arrivalLocked)
        {
            foreach (DomainParticipant entry in participants)
                if (entry.pawn is Pawn pawn && !pawn.Dead && pawn.Spawned && pawn.Map == map
                    && pawn.stances.stunner.StunTicksLeft < 90) pawn.stances.stunner.StunFor(90, caster, false, false, true);
            return;
        }
        if (caster == null || caster.Dead || caster.Downed || !worker.CasterIsPresent)
        {
            RequestReturn(caster == null ? "caster missing" : caster.Dead ? "caster died" : caster.Downed ? "caster downed" : "caster left domain");
            return;
        }
        worker.Tick();
    }

    public override void MapComponentUpdate()
    {
        challenger?.MapComponentUpdate();
        worker?.Update();
        if (!active || !closing || returnQueued) return;
        returnQueued = true;
        CQFDomainRuntime.Instance.QueueReturn(this);
    }

    public override void MapComponentDraw()
    {
        challenger?.MapComponentDraw();
        if (Find.CurrentMap == map) contest?.Draw(this);
        if (!active || Find.CurrentMap != map) return;
        LastRenderedFrame = Time.frameCount;
        worker?.Draw();
    }

    public override void MapComponentOnGUI()
    {
        worker?.OnGUI();
        challenger?.MapComponentOnGUI();
        DrawScreenBorder();
    }

    public override void FinalizeInit()
    {
        challenger?.FinalizeInit();
        if (active && caster != null)
        {
            Comp_DomainProtection.Ensure(caster);
            foreach (Hediff_DomainProtection protection in caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>())
                protection.EnsureProtection();
        }
        worker?.FinalizeInit();
    }

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref definition, "domainDefinition");
        Scribe_Values.Look(ref active, "active");
        Scribe_Values.Look(ref closing, "closing");
        Scribe_Values.Look(ref collapsed, "collapsed");
        Scribe_Values.Look(ref trapped, "trapped");
        Scribe_Values.Look(ref health, "domainHealth");
        Scribe_Values.Look(ref arrivalLocked, "arrivalLocked");
        Scribe_Values.Look(ref sourceOrigin, "sourceOrigin");
        Scribe_References.Look(ref caster, "caster", saveDestroyedThings: true);
        Scribe_References.Look(ref sourceMap, "sourceMap");
        Scribe_References.Look(ref anchor, "domainAnchor");
        Scribe_Collections.Look(ref participants, "participants", LookMode.Deep);
        Scribe_Deep.Look(ref layout, "layout");
        if (!independentInstance)
        {
            Scribe_Deep.Look(ref challenger, "challenger", map, true);
            Scribe_Deep.Look(ref contest, "contest");
        }
        if (Scribe.mode == LoadSaveMode.LoadingVars && definition != null)
        {
            worker = Activator.CreateInstance(definition.workerClass) as DomainWorker
                ?? throw new InvalidOperationException("[CQF] Could not load domain worker: " + definition.workerClass);
            worker.Domain = this;
        }
        if (worker != null && Scribe.EnterNode("domainWorker"))
        {
            try { worker.ExposeData(); }
            finally { Scribe.ExitNode(); }
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            participants ??= new();
            layout?.Bind(this);
            if (trapped && (active || caster == null))
                throw new InvalidOperationException("[CQF] Saved captive domain has inconsistent activity or caster identity.");
            if (!independentInstance && ((challenger == null) != (contest == null)
                || trapped && challenger?.Active != true || challenger?.trapped == true && !active
                || contest != null && contest.Partition.CellCount != map.Size.x * map.Size.z
                || challenger != null && active && challenger.Active && caster == challenger.Caster))
                throw new InvalidOperationException("[CQF] Saved domain contest has inconsistent participants or territory.");
        }
    }

    public override void MapRemoved()
    {
        foreach (DomainOpening opening in openings)
            if (opening is DomainOpening_Basic basic) basic.ReleaseVisuals();
        openings.Clear();
        ReleaseScreenBorder();
        challenger?.ReleaseScreenBorder();
        challenger?.Worker?.Dispose();
        contest?.Dispose();
        (Current.Game?.GetComponent<GameComponent_MapManager>()?.Module as CQFDomainRuntime)?.UnregisterDomainMap(map);
        worker?.Dispose();
        base.MapRemoved();
    }

    private void FinishReturn(Map destination, bool visible, bool sharedBattlefield)
    {
        DomainWorker returnWorker = worker ?? throw new InvalidOperationException("[CQF] Returning domain has no worker.");
        EndArrivalTransition();
        Battlefield.Layout?.ReleaseOccupants();
        Battlefield.Challenger?.Layout?.ReleaseOccupants();
        if (!sharedBattlefield && CaptiveDomain is { } captive)
            captive.FinishReturn(captive.ResolveReturnMap()
                ?? throw new InvalidOperationException("[CQF] Captive domain has no return map."), false, true);
        if (!trapped) returnWorker.Dissolved();
        ReleaseScreenBorder();
        if (caster != null)
            foreach (Hediff_DomainProtection protection in caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>()
                .Where(protection => protection.Domain == this).ToList()) caster.health.RemoveHediff(protection);
        List<Pawn> returnedPawns = new();
        foreach (DomainParticipant entry in participants)
        {
            Pawn? pawn = entry.pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Spawned || pawn.ParentHolder != null
                || Find.WorldPawns.Contains(pawn)) continue;
            IntVec3 target = destination == sourceMap ? entry.originalPosition : sourceOrigin;
            GenSpawn.Spawn(pawn, FindFreeCell(destination, target, true), destination);
            returnWorker.PawnLeaving(pawn);
            RestorePawnState(pawn, entry, destination);
            returnedPawns.Add(pawn);
        }
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn => !sharedBattlefield || OwnsParticipant(pawn)).ToList())
        {
            returnWorker.PawnLeaving(pawn);
            DomainParticipant? entry = participants.FirstOrDefault(candidate => candidate.pawn == pawn);
            IntVec3 target = destination == sourceMap && entry != null ? entry.originalPosition : sourceOrigin;
            IntVec3 cell = FindFreeCell(destination, target, true);
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.DeSpawn();
            GenSpawn.Spawn(pawn, cell, destination);
            returnedPawns.Add(pawn);
            RestorePawnState(pawn, entry, destination);
        }
        foreach (Thing thing in map.listerThings.AllThings.Where(thing => (thing is Corpse || thing.def.category == ThingCategory.Item)
            && (!sharedBattlefield || thing is Corpse body && OwnsParticipant(body.InnerPawn))).ToList())
        {
            if (thing is Corpse body) worker.PawnLeaving(body.InnerPawn);
            DomainParticipant? entry = thing is Corpse corpse
                ? participants.FirstOrDefault(candidate => candidate.pawn == corpse.InnerPawn) : null;
            IntVec3 target = destination == sourceMap && entry != null ? entry.originalPosition : sourceOrigin;
            IntVec3 cell = FindFreeCell(destination, target, false);
            thing.DeSpawn();
            GenSpawn.Spawn(thing, cell, destination);
        }
        anchor?.RemoveNormally();
        anchor = null;
        IntVec3 focus = caster != null && caster.Spawned && caster.Map == destination
            ? caster.Position : FindFreeCell(destination, sourceOrigin, false);
        if (visible)
        {
            Find.Selector.ClearSelection();
            Current.Game.CurrentMap = destination;
            Find.CameraDriver.JumpToCurrentMapLoc(focus);
            if (caster != null && caster.Spawned && caster.Map == destination) Find.Selector.Select(caster);
        }
        returnWorker.Returned(destination, focus, returnedPawns);
        layout?.Clear();
        layout = null;
        CQFDomainRuntime.Instance.Recycle(this, destination);
        active = false;
        closing = false;
        trapped = false;
        caster = null;
        sourceMap = null;
        sourceOrigin = IntVec3.Invalid;
        participants.Clear();
        health = 0f;
    }

    private void RestorePawnState(Pawn pawn, DomainParticipant? entry, Map destination)
    {
        if (pawn.drafter != null && entry != null) pawn.drafter.Drafted = entry.drafted;
        if (entry?.originalLord != null && destination == sourceMap
            && destination.lordManager.lords.Contains(entry.originalLord) && !pawn.Downed && !pawn.Dead)
            entry.originalLord.AddPawn(pawn);
    }

    private DomainDef? definition;
    private DomainWorker? worker;
    private bool active;
    private readonly bool independentInstance;
    private bool entering;
    private bool closing;
    private bool collapsed;
    private bool arrivalLocked;
    private bool returnQueued;
    private bool returnErrorReported;
    private float health;
    private Pawn? caster;
    private Map? sourceMap;
    private IntVec3 sourceOrigin;
    private Building_Domain? anchor;
    private List<DomainParticipant> participants = new();
    private DomainLayout? layout;
    private readonly HashSet<DomainOpening> openings = new();
}
