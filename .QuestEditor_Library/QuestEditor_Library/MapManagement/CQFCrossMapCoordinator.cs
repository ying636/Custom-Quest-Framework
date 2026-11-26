#if RIMWORLD_1_7
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library;

internal sealed class CQFCrossMapCoordinator : IExposable, IThingHolder
{
    public CQFCrossMapCoordinator()
    {
        cargo = new ThingOwner<Thing>(this);
    }

    public IThingHolder? ParentHolder => null;

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            foreach (Thing thing in haulTargets.Where(entry => entry.Key.Destroyed || entry.Value.Disposed)
                .Select(entry => entry.Key).ToArray())
                haulTargets.Remove(thing);
        }
        Scribe_Deep.Look(ref cargo!, "cargo", this);
        Scribe_Collections.Look(ref orders, "orders", LookMode.Deep);
        Scribe_Collections.Look(ref haulTargets, "haulTargets", LookMode.Reference, LookMode.Reference, ref savedThings, ref savedTargets);
        Scribe_Collections.Look(ref reports, "reports", LookMode.Value, LookMode.Value);
        orders ??= new();
        haulTargets ??= new();
        reports ??= new();
    }

    public ThingOwner GetDirectlyHeldThings() => cargo;
    public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, cargo);

    internal void Restore()
    {
        bySubject.Clear();
        due.Clear();
        foreach (CQFCrossMapOrder order in orders.ToArray())
        {
            if (order.subject == null || order.subject.Destroyed)
            {
                orders.Remove(order);
                continue;
            }
            bySubject.Add(order.subject, order);
            if (order.dueTick != int.MaxValue) Schedule(order, order.dueTick);
        }
    }

    internal void NotifyActivated(Map map)
    {
        foreach (CQFCrossMapOrder order in orders)
            if (order.current == map && cargo.Contains(order.subject))
                Schedule(order, Find.TickManager.TicksGame);
    }

    internal void NotifyMapRemoved(Map map)
    {
        int tick = Find.TickManager.TicksGame;
        foreach (CQFCrossMapOrder order in orders)
            if (order.current == map || order.destination == map) Schedule(order, tick);
        foreach (Thing thing in haulTargets.Where(entry => entry.Value == map || entry.Key.Map == map)
            .Select(entry => entry.Key).ToArray())
            haulTargets.Remove(thing);
    }

    internal void DesignateHaul(Thing thing, Map destination) => haulTargets[thing] = destination;
    internal Map? HaulDestination(Thing thing) => haulTargets.GetValueOrDefault(thing);

    internal bool LoadCargo(GameComponent_MapManager manager, Pawn hauler, Thing designated)
    {
        if (!haulTargets.TryGetValue(designated, out Map? target) ||
            hauler.carryTracker.CarriedThing is not { } item ||
            bySubject.ContainsKey(item) ||
            !manager.TryGetCrossMapStep(hauler.Map, target, null, out CQFCrossMapStep step) ||
            hauler.Position.DistanceToSquared(step.Portal?.Position ?? step.Departure) > 8)
            return false;
        if (!hauler.carryTracker.innerContainer.TryTransferToContainer(item, cargo, canMergeWithExistingStacks: false))
            return false;
        Map source = hauler.Map;
        if (item == designated || designated.Map != source)
        {
            source.designationManager.TryRemoveDesignationOn(designated, QEDefOf.QE_MoveIn);
            source.designationManager.TryRemoveDesignationOn(designated, QEDefOf.QE_MoveOut);
            source.designationManager.TryRemoveDesignationOn(designated, QEDefOf.QE_MoveToRoot);
            haulTargets.Remove(designated);
        }
        CQFCrossMapOrder order = Start(item, source, target, CQFCrossMapOrderKind.Haul, Find.TickManager.TicksGame);
        order.cell = hauler.Position;
        order.reachedDeparture = true;
        return true;
    }

    internal Job? HaulJob(GameComponent_MapManager manager, Pawn hauler, Thing item)
    {
        if (HaulDestination(item) is not { } target ||
            !manager.TryGetCrossMapStep(hauler.Map, target, null, out CQFCrossMapStep step)) return null;
        Job job = JobMaker.MakeJob(CQFCrossMapDefs.CQF_CrossMapHaul, item,
            step.Portal == null ? new LocalTargetInfo(step.Departure) : new LocalTargetInfo(step.Portal), item);
        job.count = item.stackCount;
        return job;
    }

    internal Job? OrderJob(GameComponent_MapManager manager, Pawn pawn, Map target)
    {
        if (!manager.TryGetCrossMapStep(pawn.Map, target, pawn, out CQFCrossMapStep step)) return null;
        if (bySubject.TryGetValue(pawn, out CQFCrossMapOrder? running))
            return running.current == pawn.Map && running.destination == target && running.kind == CQFCrossMapOrderKind.Move
                ? WalkJob(step) : null;
        Start(pawn, pawn.Map, target, CQFCrossMapOrderKind.Move, Find.TickManager.TicksGame);
        return WalkJob(step);
    }

    internal bool RequestMove(GameComponent_MapManager manager, Pawn pawn, Map source, Map target)
    {
        if (pawn.Dead || pawn.Downed || !manager.ContainsCrossMapThing(source, pawn)) return false;
        if (bySubject.TryGetValue(pawn, out CQFCrossMapOrder? order))
            return order.destination == target && order.kind == CQFCrossMapOrderKind.Move;
        if (!manager.TryGetCrossMapStep(source, target, pawn, out _)) return false;
        Start(pawn, source, target, CQFCrossMapOrderKind.Move, Find.TickManager.TicksGame);
        return true;
    }

    internal Map? TravelingMap(Pawn pawn) =>
        bySubject.TryGetValue(pawn, out var active) && cargo.Contains(pawn) ? active.current : null;

    internal bool HasPawnOrder(Pawn pawn) => bySubject.ContainsKey(pawn);

    internal bool KeepsOrganization(Pawn pawn, MetaLord owner) =>
        bySubject.TryGetValue(pawn, out var active) && owner.IncludesMap(active.current) && owner.IncludesMap(active.destination);

    internal bool RequestOrganizationMove(GameComponent_MapManager manager, MetaLord owner, Pawn pawn, Map source, Map target)
    {
        if (bySubject.TryGetValue(pawn, out var active))
            return active.organization == owner && active.destination == target;
        if (!manager.ContainsCrossMapThing(source, pawn) || pawn.Dead || pawn.Destroyed) return false;
        var order = Start(pawn, source, target, CQFCrossMapOrderKind.Organization, Find.TickManager.TicksGame);
        order.organization = owner;
        order.expires = int.MaxValue;
        return true;
    }

    internal Job? OrganizationJob(GameComponent_MapManager manager, MetaLord owner, Pawn pawn)
    {
        if (!bySubject.TryGetValue(pawn, out var active) || active.organization != owner ||
            pawn.Map != active.current || pawn.Downed || pawn.Drafted || pawn.InMentalState || pawn.IsPrisoner ||
            !manager.TryGetOrganizationStep(owner, active.current, active.destination, pawn, out CQFCrossMapStep step))
            return null;
        return WalkJob(step);
    }

    internal void StopOrganizationMove(Pawn pawn, MetaLord owner)
    {
        if (!bySubject.TryGetValue(pawn, out var active) || active.organization != owner) return;
        if (cargo.Contains(pawn))
        {
            active.organization = null;
            active.kind = CQFCrossMapOrderKind.Move;
            active.destination = active.current.Disposed ? active.origin : active.current;
            active.delivered = true;
            Schedule(active, Find.TickManager.TicksGame);
            return;
        }
        Complete(active);
        if (pawn.Spawned && pawn.CurJobDef == CQFCrossMapDefs.CQF_CrossMapWalk)
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal Job? ReportJob(GameComponent_MapManager manager, Pawn pawn, Map receiver, string groupTag,
        int responders, int assemblyTicks, int cooldownTicks)
    {
        Map source = pawn.Map;
        if (pawn.Faction == null || string.IsNullOrWhiteSpace(groupTag) || bySubject.ContainsKey(pawn) ||
            !manager.TryGetCrossMapStep(source, receiver, pawn, out CQFCrossMapStep step))
            return null;
        int now = Find.TickManager.TicksGame;
        string key = source.uniqueID + "/" + receiver.uniqueID + "/" + pawn.Faction.loadID;
        if (reports.GetValueOrDefault(key) > now) return null;
        var order = Start(pawn, source, receiver, CQFCrossMapOrderKind.Report, now);
        order.reportOrigin = source;
        order.reinforcementGroupTag = groupTag.Trim();
        order.responders = Math.Clamp(responders, 1, 32);
        order.assemblyTicks = Math.Clamp(assemblyTicks, 0, 60000);
        reports[key] = now + Math.Clamp(cooldownTicks, 60, 60000);
        return WalkJob(step);
    }

    internal List<Pawn>? FindPursuers(Pawn target, Map source)
    {
        if (!target.IsColonistPlayerControlled) return null;
        List<Pawn>? result = null;
        foreach (Pawn pawn in source.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Downed || pawn.Dead || pawn == target || bySubject.ContainsKey(pawn)) continue;
            bool predation = pawn.CurJob?.def == JobDefOf.PredatorHunt && pawn.CurJob.targetA.Thing == target;
            if (!predation && (!pawn.HostileTo(target) || pawn.mindState.enemyTarget != target)) continue;
            if (pawn.Position.DistanceToSquared(target.Position) > 24 * 24) continue;
            (result ??= new()).Add(pawn);
        }
        return result;
    }

    internal void Pursue(List<Pawn>? pursuers, Pawn target, Map source, Map destination, IntVec3 exit)
    {
        if (pursuers == null) return;
        int now = Find.TickManager.TicksGame;
        foreach (Pawn pawn in pursuers)
        {
            if (pawn.Map == source && !bySubject.ContainsKey(pawn))
            {
                var order = Start(pawn, source, destination, CQFCrossMapOrderKind.Pursuit, now);
                order.quarry = target;
                order.preferredExit = exit;
            }
        }
    }

    internal void ReachedDeparture(Pawn pawn, IntVec3 departure)
    {
        if (!bySubject.TryGetValue(pawn, out CQFCrossMapOrder? order)) return;
        order.reachedDeparture = true;
        order.cell = departure;
        Schedule(order, Find.TickManager.TicksGame);
    }

    internal void Update(GameComponent_MapManager manager)
    {
        if (due.Count == 0 || LongEventHandler.AnyEventNowOrWaiting) return;
        int now = Find.TickManager.TicksGame;
        int budget = 32;
        while (budget-- > 0 && due.TryPeek(out var next, out int tick) && tick <= now)
        {
            due.Dequeue();
            CQFCrossMapOrder order = next.Order;
            if (next.Version != order.scheduled || bySubject.GetValueOrDefault(order.subject) != order) continue;
            try { Advance(manager, order, now); }
            catch (Exception error)
            {
                Log.Error("[CQF] Cross-map transfer failed for " + order.subject.GetUniqueLoadID() + ": " + error);
                Schedule(order, now + 120);
            }
        }
    }

    private void Advance(GameComponent_MapManager manager, CQFCrossMapOrder order, int now)
    {
        Thing subject = order.subject;
        if (subject.Destroyed || subject is Pawn { Dead: true })
        {
            Complete(order);
            return;
        }
        bool held = cargo.Contains(subject);
        if (order.kind == CQFCrossMapOrderKind.Organization && subject is Pawn ordered &&
            (order.organization == null || order.organization.Disposed || order.organization.GetMove(ordered)?.targetMap != order.destination))
        {
            if (order.organization != null) StopOrganizationMove(ordered, order.organization);
            else if (!held) Complete(order);
            else
            {
                order.kind = CQFCrossMapOrderKind.Move;
                order.destination = order.current;
                Schedule(order, now);
            }
            return;
        }
        if (!held && !manager.ContainsCrossMapThing(order.current, subject))
        {
            Complete(order);
            return;
        }
        if (order.destination.Disposed || order.current.Disposed || (now >= order.expires && !order.delivered) ||
            (order.kind == CQFCrossMapOrderKind.Pursuit && order.quarry is not { Dead: false, Destroyed: false }))
        {
            if (!held)
            {
                Complete(order);
                return;
            }
            if (order.current.Disposed) order.current = !order.origin.Disposed ? order.origin : order.destination;
            order.destination = order.current;
            order.kind = CQFCrossMapOrderKind.Move;
            order.cell = order.current.Center;
            order.delivered = true;
            if (order.current.Disposed)
            {
                order.dueTick = int.MaxValue;
                Log.Error("[CQF] Both endpoints of an in-transit object have been removed: " + subject.GetUniqueLoadID());
                return;
            }
        }

        bool sleeping = manager.IsHibernating(order.current);
        if (order.current == order.destination)
        {
            if (!sleeping && held && !Land(manager, order))
            {
                Schedule(order, now + 60);
                return;
            }
            if (!order.delivered && order.kind == CQFCrossMapOrderKind.Report)
            {
                order.delivered = true;
                DeliverReport(manager, order, now);
            }
            if (sleeping)
            {
                order.dueTick = int.MaxValue;
                order.delivered = true;
                return;
            }
            Complete(order);
            return;
        }

        Pawn? pawn = order.kind != CQFCrossMapOrderKind.Haul ? subject as Pawn : null;
        if (!held && order.kind == CQFCrossMapOrderKind.Reinforcement && order.current == order.origin &&
            pawn is { } guard && (guard.IsPrisoner || !order.origin.lordManager.lords.Any(lord =>
                IsReinforcementGroup(lord, guard.Faction, order.reinforcementGroupTag) && lord.ownedPawns.Contains(guard))))
        {
            Complete(order);
            if (!sleeping && guard.CurJobDef == CQFCrossMapDefs.CQF_CrossMapWalk)
                guard.jobs.EndCurrentJob(JobCondition.InterruptForced);
            return;
        }
        if (held && !sleeping && pawn != null)
        {
            if (!Land(manager, order))
            {
                Schedule(order, now + 60);
                return;
            }
            if (bySubject.GetValueOrDefault(subject) != order) return;
            held = false;
            order.marchUntil = 0;
        }
        CQFCrossMapStep step = default;
        bool blocked = pawn is { Downed: true } || (order.organization != null && pawn is { } member &&
            (member.Drafted || member.InMentalState || member.IsPrisoner));
        bool hasStep = !blocked && (order.organization is { } organization
            ? manager.TryGetOrganizationStep(organization, order.current, order.destination, pawn!, out step)
            : manager.TryGetCrossMapStep(order.current, order.destination, pawn, out step, order.preferredExit.IsValid ? order.preferredExit : null));
        MoveRequirment? move = pawn != null ? order.organization?.GetMove(pawn) : null;
        if (!hasStep)
        {
            if (move != null)
            {
                move.status = MetaLordMoveStatus.Waiting;
                move.reason = blocked ? "PawnUnavailable" : "PassageUnavailable";
            }
            Schedule(order, now + 120);
            return;
        }
        if (move != null)
        {
            move.status = MetaLordMoveStatus.Moving;
            move.reason = null;
        }
        if (!sleeping && pawn != null && !held)
        {
            bool atDeparture = step.Portal == null ? pawn.Position == step.Departure :
                pawn.Position.DistanceToSquared(step.Portal.Position) <= 8;
            if (!order.reachedDeparture || step.Departure != order.cell || !atDeparture)
            {
                if (pawn.CurJobDef != CQFCrossMapDefs.CQF_CrossMapWalk && pawn.CurJobDef != QEDefOf.CQF_MetaLordMove)
                {
                    var target = step.Portal != null ? new LocalTargetInfo(step.Portal) : new LocalTargetInfo(step.Departure);
                    if (pawn.CanReach(target, step.Portal != null ? PathEndMode.Touch : PathEndMode.OnCell, Danger.Deadly))
                        pawn.jobs.StartJob(WalkJob(step), JobCondition.InterruptOptional);
                }
                Schedule(order, now + 60);
                return;
            }
        }
        else if (!order.reachedDeparture && order.marchUntil == 0)
        {
            IntVec3 from = held ? order.cell : subject.Position;
            order.marchUntil = now + Math.Max(15, (Math.Abs(from.x - step.Departure.x) +
                Math.Abs(from.z - step.Departure.z)) * 13);
            Schedule(order, order.marchUntil);
            return;
        }
        order.reachedDeparture = false;
        order.marchUntil = 0;
        if (order.organization != null && pawn != null && step.Portal is CustomMapEntrance or CustomMapExit)
        {
            TraverseOrganizationPortal(manager, order, pawn, step, now, held, sleeping);
            return;
        }
        if (!held)
        {
            try { manager.DetachCrossMapThing(order.current, subject); }
            catch
            {
                if (!manager.ContainsCrossMapThing(order.current, subject) && !subject.Spawned && !subject.Destroyed)
                    cargo.TryAdd(subject, canMergeWithExistingStacks: false);
                throw;
            }
            if (!cargo.TryAdd(subject, canMergeWithExistingStacks: false))
                throw new InvalidOperationException("[CQF] Could not hold the traveling object.");
        }
        order.current = step.Destination;
        order.cell = step.Arrival;
        Schedule(order, now);
    }

    private void TraverseOrganizationPortal(GameComponent_MapManager manager, CQFCrossMapOrder order, Pawn pawn,
        CQFCrossMapStep step, int now, bool held, bool sleeping)
    {
        Map source = order.current;
        if (sleeping) manager.Activate(source);
        try
        {
            if (held && !Land(manager, order))
            {
                Schedule(order, now + 60);
                return;
            }
            if (bySubject.GetValueOrDefault(pawn) != order) return;
            var portal = (CQFMapPortal)step.Portal!;
            if (portal.IsAvailable(pawn) && portal.IsEnterable(out _))
            {
                if (portal is CustomMapEntrance entrance) entrance.TryEnter(pawn);
                else ((CustomMapExit)portal).Exit(pawn);
            }
        }
        finally
        {
            if (pawn.Spawned)
            {
                order.current = pawn.Map;
                order.cell = pawn.Position;
            }
            if (sleeping && !source.Disposed && pawn.Map != source) manager.Hibernate(source);
        }
        if (bySubject.GetValueOrDefault(pawn) == order)
            Schedule(order, now + (order.current == step.Destination ? 1 : 120));
    }

    private void DeliverReport(GameComponent_MapManager manager, CQFCrossMapOrder order, int now)
    {
        if (order.subject is not Pawn messenger || messenger.Faction == null ||
            string.IsNullOrWhiteSpace(order.reinforcementGroupTag) ||
            order.reportOrigin is not { Disposed: false } target) return;
        if (!manager.TryGetCrossMapStep(order.destination, target, null, out _)) return;
        int count = order.responders;
        foreach (Lord lord in order.destination.lordManager.lords)
        {
            if (!IsReinforcementGroup(lord, messenger.Faction, order.reinforcementGroupTag)) continue;
            foreach (Pawn guard in lord.ownedPawns)
            {
                if (guard.Dead || guard.Downed || guard.IsPrisoner || guard.Faction != messenger.Faction ||
                    !manager.ContainsCrossMapThing(order.destination, guard) || bySubject.ContainsKey(guard)) continue;
                var reinforcement = Start(guard, order.destination, target, CQFCrossMapOrderKind.Reinforcement, now + order.assemblyTicks);
                reinforcement.reinforcementGroupTag = order.reinforcementGroupTag;
                if (--count <= 0) return;
            }
        }
    }

    private static bool IsReinforcementGroup(Lord lord, Faction faction, string tag) =>
        lord.faction == faction && lord.LordJob is MultLevelLordJobBase group && group.groupTags.Contains(tag);

    private bool Land(GameComponent_MapManager manager, CQFCrossMapOrder order)
    {
        Map destination = order.current;
        Thing subject = order.subject;
        if (!manager.TryCrossMapArrival(destination, subject, order.cell, out IntVec3 arrival)) return false;
        cargo.Remove(subject);
        try { GenSpawn.Spawn(subject, arrival, destination, order.rotation, WipeMode.VanishOrMoveAside); }
        catch
        {
            if (!subject.Spawned) cargo.TryAdd(subject, canMergeWithExistingStacks: false);
            throw;
        }
        if (subject is Pawn pawn)
        {
            var owner = Current.Game.GetComponent<GameComponent_MetaLord>().Manager.GetForPawn(pawn);
            owner?.PawnArrivedNewLevel(pawn, destination);
            if (order.kind == CQFCrossMapOrderKind.Reinforcement && destination == order.destination &&
                pawn.HostileTo(Faction.OfPlayer) && pawn.GetLord() == null)
            {
                Lord? assault = destination.lordManager.lords.FirstOrDefault(lord =>
                    lord.faction == pawn.Faction && lord.LordJob is LordJob_AssaultColony);
                if (assault == null)
                    LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction), destination, new[] { pawn });
                else
                    assault.AddPawn(pawn);
            }
            if (order.quarry is { Spawned: true } target && target.Map == destination &&
                GenSight.LineOfSight(pawn.Position, target.Position, destination))
                pawn.mindState.enemyTarget = target;
        }
        return true;
    }

    private CQFCrossMapOrder Start(Thing subject, Map source, Map destination, CQFCrossMapOrderKind kind, int tick)
    {
        var order = new CQFCrossMapOrder
        {
            subject = subject,
            origin = source,
            current = source,
            destination = destination,
            cell = subject.Position,
            rotation = subject.Rotation,
            kind = kind,
            expires = tick + (kind == CQFCrossMapOrderKind.Pursuit ? 1200 : 60000)
        };
        orders.Add(order);
        bySubject.Add(subject, order);
        Schedule(order, tick);
        return order;
    }

    private static Job WalkJob(CQFCrossMapStep step) => JobMaker.MakeJob(CQFCrossMapDefs.CQF_CrossMapWalk,
        step.Departure, step.Portal == null ? LocalTargetInfo.Invalid : new LocalTargetInfo(step.Portal));

    private void Schedule(CQFCrossMapOrder order, int tick)
    {
        order.dueTick = tick;
        due.Enqueue((order, ++order.scheduled), tick);
    }

    private void Complete(CQFCrossMapOrder order)
    {
        if (bySubject.GetValueOrDefault(order.subject) == order)
            bySubject.Remove(order.subject);
        orders.Remove(order);
    }

    private ThingOwner<Thing> cargo;
    private List<CQFCrossMapOrder> orders = new();
    private Dictionary<Thing, Map> haulTargets = new();
    private Dictionary<string, int> reports = new();
    private List<Thing>? savedThings;
    private List<Map>? savedTargets;
    private readonly Dictionary<Thing, CQFCrossMapOrder> bySubject = new();
    private readonly PriorityQueue<(CQFCrossMapOrder Order, int Version), int> due = new();
}
#endif
