using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class MetaLord : IExposable, ILoadReferenceable, IDisposable
    {
        public bool Disposed => this.disposed;
        public IReadOnlyList<Pawn> Members => this.members.AsReadOnly();
        public IReadOnlyList<LevelLord> Levels => this.levelLords.AsReadOnly();
        public IReadOnlyList<MoveRequirment> Moves => this.moves.AsReadOnly();
        public IReadOnlyList<MoveRequirment> RecentMoves => this.recentMoves.AsReadOnly();

        public Lord CreateGroup(Map map, string groupName, DutyMapDef dutyMap = null, int capacity = 0, List<string> tags = null)
        {
            this.ValidateMap(map);
            if (string.IsNullOrWhiteSpace(groupName) || capacity < 0)
                throw new ArgumentException("[CQF] MetaLord group requires a name and nonnegative capacity.");
            LevelLord level = this.levelLords.Find(l => l.level == map);
            Lord existing = level?.ownLords.Find(l => (l.LordJob as MultLevelLordJobBase)?.groupName == groupName);
            if (existing != null)
                throw new InvalidOperationException("[CQF] Duplicate MetaLord group: " + this.name + "/" + groupName);
            return LordMaker.MakeNewLord(this.faction, new LordJob_MetaLord
            {
                metaLord = this, groupName = groupName, defaultDutyMap = dutyMap,
                capacity = capacity, groupTags = tags == null ? new List<string>() : new List<string>(tags)
            }, map);
        }

        public void RegisterGroup(Lord lord)
        {
            this.ValidateMap(lord?.Map);
            if (lord.faction != this.faction || !(lord.LordJob is MultLevelLordJobBase job) || job.metaLord != this ||
                string.IsNullOrWhiteSpace(job.groupName) || job.capacity < 0)
                throw new InvalidOperationException("[CQF] Invalid MetaLord group binding: " + this.name);
            LevelLord level = this.levelLords.Find(l => l.level == lord.Map);
            if (level == null)
            {
                level = new LevelLord { level = lord.Map };
                this.levelLords.Add(level);
            }
            if (level.ownLords.Contains(lord)) return;
            if (level.ownLords.Any(l => (l.LordJob as MultLevelLordJobBase)?.groupName == job.groupName))
                throw new InvalidOperationException("[CQF] Duplicate MetaLord group: " + job.groupName);
            level.ownLords.Add(lord);
        }

        public void UnregisterGroup(Lord lord)
        {
            LevelLord level = this.levelLords.Find(l => l.ownLords.Contains(lord));
            if (level == null) return;
            level.ownLords.Remove(lord);
            this.Trigger(new LordEventSignal(LordEventSignalType.GroupRemoved) { group = lord, sourceMap = level.level });
        }

        public void RemoveGroup(Lord lord)
        {
            if (lord == null || !(lord.LordJob is MultLevelLordJobBase job) || job.metaLord != this)
                throw new ArgumentException("[CQF] Cannot remove a group outside this MetaLord.");
            if (lord.Map.lordManager.lords.Contains(lord))
                lord.Map.lordManager.RemoveLord(lord);
            this.UnregisterGroup(lord);
        }

        public void AddMember(Pawn pawn, string group = null)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Faction != this.faction || !pawn.Spawned || pawn.IsPrisoner)
                throw new ArgumentException("[CQF] Invalid MetaLord member: " + pawn);
            this.ValidateMap(pawn.Map);
            MetaLord owner = this.manager.GetForPawn(pawn);
            if (owner != null && owner != this)
                throw new InvalidOperationException("[CQF] Pawn is already in another MetaLord: " + pawn);
            Lord target = this.SelectGroup(pawn, pawn.Map, group, true);
            if (target == null)
                throw new InvalidOperationException("[CQF] No MetaLord group accepts member: " + pawn);
            this.Transfer(pawn, target);
        }

        public void NotifyMemberAdded(Pawn pawn)
        {
            if (this.disposed) return;
            if (pawn == null || pawn.Faction != this.faction)
                throw new InvalidOperationException("[CQF] MetaLord received an invalid member: " + pawn);
            bool known = this.manager.GetForPawn(pawn) == this;
            this.manager.Bind(pawn, this);
            if (known) return;
            this.members.Add(pawn);
            this.memberRevision++;
            this.Trigger(new LordEventSignal(LordEventSignalType.PawnJoined)
            { pawn = pawn, group = pawn.GetLord(), targetMap = pawn.Map });
        }

        public void RemoveMember(Pawn pawn, string reason = "Removed")
        {
            if (pawn == null || !this.members.Remove(pawn)) return;
            this.memberRevision++;
            Lord previous = pawn.GetLord();
            this.manager.Unbind(pawn, this);
            if ((previous?.LordJob as MultLevelLordJobBase)?.metaLord == this)
                previous.RemovePawn(pawn);
            MoveRequirment move = this.GetMove(pawn);
            if (move != null) this.FinishMove(move, MetaLordMoveStatus.Cancelled, reason);
            this.Trigger(new LordEventSignal(pawn.Dead ? LordEventSignalType.PawnKilled : LordEventSignalType.PawnLeft)
            { pawn = pawn, group = previous, sourceMap = pawn.Map, reason = reason });
        }

        public MoveRequirment RequestMove(Pawn pawn, Map destination, string group = null, int timeoutTicks = 60000)
        {
            this.ValidateMap(destination);
            if (this.manager.GetForPawn(pawn) != this || pawn == null || !pawn.Spawned || timeoutTicks <= 0)
                throw new ArgumentException("[CQF] MetaLord move requires a spawned member and positive timeout.");
            LevelPather.GetPathPortal(pawn.Map, destination);
            MoveRequirment previous = this.GetMove(pawn);
            if (previous != null) this.FinishMove(previous, MetaLordMoveStatus.Cancelled, "Replaced", false);
            MoveRequirment result = new MoveRequirment
            {
                pawn = pawn, targetMap = destination, targetGroup = group,
                createdTick = Find.TickManager.TicksGame, timeoutTicks = timeoutTicks
            };
            this.moves.Add(result);
            this.moveIndex[pawn] = result;
            if (previous != null) this.NotifyMoveResult(previous);
            if (this.GetMove(pawn) != result) return result;
            if (pawn.Map == destination) this.PawnArrivedNewLevel(pawn, destination);
            else this.RefreshDuty(pawn, true);
            return result;
        }

        public MoveRequirment GetMove(Pawn pawn)
        {
            return pawn != null && this.moveIndex.TryGetValue(pawn, out MoveRequirment result) ? result : null;
        }

        public void CancelMove(Pawn pawn)
        {
            MoveRequirment move = this.GetMove(pawn);
            if (move != null) this.FinishMove(move, MetaLordMoveStatus.Cancelled, "Cancelled");
        }

        public bool TryGetNextPortal(Pawn pawn, out CQFMapPortal portal)
        {
            portal = null;
            MoveRequirment move = this.GetMove(pawn);
            if (move == null || Find.TickManager.TicksGame < move.nextCheckTick) return false;
            try
            {
                if (pawn.Map == move.targetMap)
                {
                    this.PawnArrivedNewLevel(pawn, pawn.Map);
                    return false;
                }
                List<MapPortal> route = LevelPather.GetPathPortal(pawn.Map, move.targetMap);
                if (route.Count == 0 || !(route[0] is CQFMapPortal next) || next.Map != pawn.Map || !next.Spawned)
                    throw new InvalidOperationException("InvalidRoute");
                string unavailable = pawn.Downed ? "PawnDowned" : pawn.InMentalState ? "MentalState" : pawn.Drafted ? "Drafted" :
                    !next.IsAllowed(pawn) ? "LevelNotAllowed" : !next.IsAvailable(pawn) ? "PortalCooldown" :
                    !next.IsEnterable(out _) ? "PortalClosed" : !pawn.CanReach(next, PathEndMode.Touch, Danger.Deadly) ? "NoLocalPath" : null;
                if (unavailable != null)
                {
                    this.WaitMove(move, unavailable);
                    return false;
                }
                move.status = MetaLordMoveStatus.Moving;
                move.reason = null;
                portal = next;
                return true;
            }
            catch (Exception error)
            {
                this.FinishMove(move, MetaLordMoveStatus.Failed, error.Message);
                return false;
            }
        }

        public void PawnArrivedNewLevel(Pawn pawn, Map level)
        {
            if (this.disposed || this.manager.GetForPawn(pawn) != this) return;
            if (level == null || level.Disposed || LevelPather.GetRoot(level) != this.rootMap)
            {
                this.RemoveMember(pawn, "ExitedScope");
                return;
            }
            this.ValidateMap(level);
            MoveRequirment move = this.GetMove(pawn);
            bool final = move != null && move.targetMap == level;
            Lord target = this.SelectGroup(pawn, level, final ? move.targetGroup : null, true);
            if (target == null)
            {
                Lord local = pawn.GetLord();
                if (local == null || local.Map != level)
                {
                    LevelLord currentLevel = this.levelLords.Find(l => l.level == level);
                    Lord transit = currentLevel?.ownLords.Find(l => (l.LordJob as MultLevelLordJobBase)?.groupName == "CQF_Transit");
                    transit ??= this.CreateGroup(level, "CQF_Transit");
                    this.Transfer(pawn, transit);
                }
                if (move != null) this.WaitMove(move, "NoReceivingGroup");
                this.RefreshDuty(pawn, false);
                return;
            }
            Map source = pawn.GetLord()?.Map;
            this.Transfer(pawn, target);
            if (this.disposed || this.manager.GetForPawn(pawn) != this) return;
            if (pawn.GetLord()?.Map != level || (final && pawn.GetLord() != target))
            {
                if (move != null) this.WaitMove(move, "ReceiverChanged");
                return;
            }
            if (move != null && this.GetMove(pawn) == move)
            {
                if (final) this.FinishMove(move, MetaLordMoveStatus.Completed, null);
                else
                {
                    move.status = MetaLordMoveStatus.Pending;
                    move.nextCheckTick = 0;
                    this.RefreshDuty(pawn, false);
                }
            }
            this.Trigger(new LordEventSignal(LordEventSignalType.PawnArrived)
            { pawn = pawn, group = target, sourceMap = source, targetMap = level });
        }

        public void NotifyMapRemoved(Map map)
        {
            foreach (MoveRequirment move in this.moves.ToArray())
                if (move.targetMap == map) this.FinishMove(move, MetaLordMoveStatus.Failed, "MapRemoved");
            foreach (MoveRequirment move in this.recentMoves)
                if (move.targetMap == map) move.targetMap = null;
            this.levelLords.RemoveAll(l => l.level == map);
            foreach (Pawn pawn in this.members.ToArray())
                if (pawn.Map == map) this.RemoveMember(pawn, "MapRemoved");
            this.Trigger(new LordEventSignal(LordEventSignalType.MapRemoved) { sourceMap = map });
        }

        public void Tick()
        {
            if (this.rootMap == null || this.rootMap.Disposed)
            {
                this.manager.Remove(this);
                return;
            }
            int now = Find.TickManager.TicksGame;
            foreach (LevelLord level in this.levelLords.ToArray())
            {
                if (level.level == null || level.level.Disposed)
                {
                    this.NotifyMapRemoved(level.level);
                    continue;
                }
                foreach (Lord lord in level.ownLords.ToArray())
                    if (!level.level.lordManager.lords.Contains(lord) || (lord.LordJob as MultLevelLordJobBase)?.metaLord != this)
                        this.UnregisterGroup(lord);
            }
            foreach (Pawn pawn in this.members.ToArray())
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Faction != this.faction || pawn.IsPrisoner)
                {
                    this.RemoveMember(pawn, "MemberInvalid");
                    continue;
                }
                if (!pawn.Spawned) continue;
                if (LevelPather.GetRoot(pawn.Map) != this.rootMap)
                {
                    this.RemoveMember(pawn, "ExitedScope");
                    continue;
                }
                bool sleeping = GameComponent_MapManager.Instance?.IsHibernating(pawn.Map) == true;
                MoveRequirment move = this.GetMove(pawn);
                if (sleeping)
                {
                    if (move != null) move.createdTick += 60;
                    continue;
                }
                if (pawn.InMentalState || pawn.Drafted) continue;
                if (pawn.GetLord() is Lord assigned && (assigned.LordJob as MultLevelLordJobBase)?.metaLord != this)
                {
                    this.RemoveMember(pawn, "JoinedOtherLord");
                    continue;
                }
                if (pawn.GetLord() == null || pawn.GetLord().Map != pawn.Map)
                    this.PawnArrivedNewLevel(pawn, pawn.Map);
                else if (pawn.mindState.duty == null)
                    this.RefreshDuty(pawn, false);
                move = this.GetMove(pawn);
                if (move == null) continue;
                if (move.targetMap == null || move.targetMap.Disposed)
                    this.FinishMove(move, MetaLordMoveStatus.Failed, "MapRemoved");
                else if (now - move.createdTick >= move.timeoutTicks)
                    this.FinishMove(move, MetaLordMoveStatus.Failed, "Timeout");
                else if (move.targetMap == pawn.Map && now >= move.nextCheckTick)
                    this.PawnArrivedNewLevel(pawn, pawn.Map);
            }
            if (this.eventDef != null && now >= this.nextEventTick)
            {
                this.nextEventTick = now + Math.Max(60, this.eventDef.tickInterval);
                this.Trigger(new LordEventSignal(LordEventSignalType.Tick));
            }
        }

        public void Rebuild()
        {
            this.moveIndex.Clear();
            foreach (Pawn pawn in this.members.ToArray())
                if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Faction != this.faction || pawn.IsPrisoner)
                {
                    Log.Warning("[CQF] Removing invalid loaded MetaLord member: " + this.name + "/" + pawn);
                    this.members.Remove(pawn);
                }
            foreach (Pawn pawn in this.members) this.manager.Bind(pawn, this);
            foreach (LevelLord level in this.levelLords.ToArray())
            {
                if (level.level == null || level.level.Disposed)
                {
                    Log.Error("[CQF] Loaded MetaLord has an invalid level: " + this.name);
                    this.levelLords.Remove(level);
                    continue;
                }
                foreach (Lord lord in level.ownLords.ToArray())
                    if (lord == null || !level.level.lordManager.lords.Contains(lord) ||
                        !(lord.LordJob is MultLevelLordJobBase job) || job.metaLord != this || lord.Map != level.level || lord.faction != this.faction)
                    {
                        Log.Error("[CQF] Removing invalid loaded MetaLord group reference: " + this.name + "/" + lord);
                        level.ownLords.Remove(lord);
                    }
                foreach (Lord lord in level.ownLords)
                {
                    MultLevelLordJobBase job = (MultLevelLordJobBase)lord.LordJob;
                    job.metaLord = this;
                    foreach (Pawn pawn in lord.ownedPawns)
                    {
                        this.manager.Bind(pawn, this);
                        if (!this.members.Contains(pawn)) this.members.Add(pawn);
                    }
                }
            }
            foreach (MoveRequirment move in this.moves.ToArray())
            {
                if (move?.pawn == null || !this.members.Contains(move.pawn) || this.moveIndex.ContainsKey(move.pawn))
                {
                    Log.Error("[CQF] Removing invalid loaded MetaLord move: " + this.name);
                    this.moves.Remove(move);
                    continue;
                }
                this.moveIndex.Add(move.pawn, move);
                if (move.targetMap == null || move.targetMap.Disposed)
                    this.FinishMove(move, MetaLordMoveStatus.Failed, "MapRemoved");
            }
            foreach (Pawn pawn in this.members)
                if (pawn.Spawned && GameComponent_MapManager.Instance?.IsHibernating(pawn.Map) != true)
                    this.RefreshDuty(pawn, false);
        }

        public void Dispose()
        {
            if (this.disposed) return;
            if (this.manager?.Lords.Contains(this) == true)
            {
                this.manager.Remove(this);
                return;
            }
            this.disposed = true;
            foreach (Pawn pawn in this.members.ToArray())
            {
                this.manager?.Unbind(pawn, this);
                if (pawn.CurJobDef == QEDefOf.CQF_MetaLordMove)
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            foreach (LevelLord level in this.levelLords)
                foreach (Lord lord in level.ownLords.ToArray())
                    if (level.level != null && !level.level.Disposed && level.level.lordManager.lords.Contains(lord))
                        if ((lord.LordJob as MultLevelLordJobBase)?.metaLord == this)
                            level.level.lordManager.RemoveLord(lord);
            this.members.Clear();
            foreach (MoveRequirment move in this.moves)
            {
                move.status = MetaLordMoveStatus.Cancelled;
                move.reason = "Disbanded";
            }
            this.moves.Clear();
            this.moveIndex.Clear();
            this.levelLords.Clear();
            this.eventQueue.Clear();
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                this.recentMoves.RemoveAll(m => m == null || m.pawn == null || m.pawn.Destroyed || m.pawn.Dead);
            Scribe_Values.Look(ref this.loadID, "loadID");
            Scribe_Values.Look(ref this.name, "name");
            Scribe_Values.Look(ref this.nextEventTick, "nextEventTick");
            Scribe_References.Look(ref this.rootMap, "rootMap");
            Scribe_References.Look(ref this.faction, "faction");
            Scribe_References.Look(ref this.quest, "quest");
            Scribe_Defs.Look(ref this.eventDef, "eventDef");
            Scribe_Collections.Look(ref this.members, "members", LookMode.Reference);
            Scribe_Collections.Look(ref this.levelLords, "levelLords", LookMode.Deep);
            Scribe_Collections.Look(ref this.moves, "moves", LookMode.Deep);
            Scribe_Collections.Look(ref this.recentMoves, "recentMoves", LookMode.Deep);
            Scribe_Collections.Look(ref this.eventStates, "eventStates", LookMode.Value, LookMode.Deep,
                ref this.stateKeys, ref this.stateValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.members ??= new List<Pawn>();
                this.levelLords ??= new List<LevelLord>();
                this.moves ??= new List<MoveRequirment>();
                this.recentMoves ??= new List<MoveRequirment>();
                this.eventStates ??= new Dictionary<string, MetaLordEventState>();
            }
        }

        public string GetUniqueLoadID()
        {
            return "CQF_MetaLord_" + this.loadID;
        }

        public void Trigger(LordEventSignal signal)
        {
            if (this.disposed || this.eventDef == null) return;
            if (this.eventQueue.Count >= 256)
                throw new InvalidOperationException("[CQF] MetaLord event queue overflow: " + this.name);
            this.eventQueue.Enqueue(signal);
            if (this.processingEvents) return;
            this.processingEvents = true;
            try
            {
                int processed = 0;
                while (!this.disposed && this.eventQueue.Count > 0)
                {
                    if (++processed > 256)
                        throw new InvalidOperationException("[CQF] MetaLord event recursion limit: " + this.name);
                    LordEventSignal current = this.eventQueue.Dequeue();
                    foreach (MetaLordEvent entry in this.eventDef.events.ToArray())
                    {
                        if (this.disposed) break;
                        if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.trigger == null)
                        {
                            Log.Error("[CQF] Invalid MetaLord event in " + this.eventDef.defName);
                            continue;
                        }
                        if (!this.eventStates.TryGetValue(entry.key, out MetaLordEventState state))
                            this.eventStates[entry.key] = state = new MetaLordEventState();
                        if (state.faulted || (entry.once && state.executed) || Find.TickManager.TicksGame < state.nextTick) continue;
                        try
                        {
                            if (!entry.trigger.Active(this, current)) continue;
                            state.executed = true;
                            state.nextTick = Find.TickManager.TicksGame + Math.Max(0, entry.cooldownTicks);
                            Dictionary<string, TargetInfo> targets = current.MakeTargets(this);
                            foreach (CQFAction action in entry.actions.ToArray())
                            {
                                if (action == null) throw new InvalidOperationException("Null event action");
                                action.Work(targets, this.quest);
                                if (this.disposed) break;
                            }
                        }
                        catch (Exception error)
                        {
                            state.faulted = true;
                            Log.Error("[CQF] MetaLord event disabled after error: " + this.name + "/" + entry.key + "\n" + error);
                        }
                    }
                }
            }
            finally
            {
                this.eventQueue.Clear();
                this.processingEvents = false;
            }
        }

        private void ValidateMap(Map map)
        {
            if (this.disposed || map == null || map.Disposed || LevelPather.GetRoot(map) != this.rootMap)
                throw new InvalidOperationException("[CQF] Map is outside MetaLord scope: " + this.name + "/" + map);
        }

        private Lord SelectGroup(Pawn pawn, Map map, string group, bool create)
        {
            LevelLord level = this.levelLords.Find(l => l.level == map);
            if (create && string.IsNullOrEmpty(group) && (level == null || level.ownLords.Count == 0))
            {
                this.CreateGroup(map, "CQF_Default");
                level = this.levelLords.Find(l => l.level == map);
            }
            Lord best = null;
            float score = float.NegativeInfinity;
            if (level == null) return null;
            foreach (Lord lord in level.ownLords.ToArray())
            {
                if (!map.lordManager.lords.Contains(lord) || !(lord.LordJob is MultLevelLordJobBase job) ||
                    job.metaLord != this || lord.faction != pawn.Faction ||
                    (!string.IsNullOrEmpty(group) && job.groupName != group && !job.groupTags.Contains(group)) ||
                    !lord.CanAddPawn(pawn)) continue;
                float current = job.GetPawnAcceptScore(pawn);
                if (float.IsNaN(current) || current < 0 || float.IsInfinity(current)) continue;
                if (best == null || current > score) { best = lord; score = current; }
            }
            return best;
        }

        private void Transfer(Pawn pawn, Lord target)
        {
            Lord previous = pawn.GetLord();
            if (previous == target) return;
            if (previous != null) previous.RemovePawn(pawn);
            int revision = this.memberRevision;
            target.AddPawn(pawn);
            if (pawn.GetLord() != target)
            {
                if (this.disposed || this.memberRevision != revision || !target.Map.lordManager.lords.Contains(target)) return;
                if (previous != null && previous.Map == pawn.Map && previous.CanAddPawn(pawn)) previous.AddPawn(pawn);
                throw new InvalidOperationException("[CQF] MetaLord member transfer failed: " + pawn);
            }
            previous?.CurLordToil?.UpdateAllDuties();
            this.Trigger(new LordEventSignal(LordEventSignalType.PawnAssigned)
            { pawn = pawn, group = target, sourceMap = previous?.Map, targetMap = target.Map });
        }

        private void WaitMove(MoveRequirment move, string reason)
        {
            if (this.GetMove(move.pawn) != move) return;
            if (move.reason != reason)
                Log.Warning("[CQF] MetaLord move waiting: " + this.name + "/" + move.pawn + "/" + reason);
            move.status = MetaLordMoveStatus.Waiting;
            move.reason = reason;
            move.nextCheckTick = Find.TickManager.TicksGame + 120;
        }

        private void FinishMove(MoveRequirment move, MetaLordMoveStatus status, string reason, bool notify = true)
        {
            if (!this.moves.Remove(move)) return;
            this.moveIndex.Remove(move.pawn);
            move.status = status;
            move.reason = reason;
            this.recentMoves.Add(move);
            if (this.recentMoves.Count > 32) this.recentMoves.RemoveAt(0);
            this.RefreshDuty(move.pawn, status != MetaLordMoveStatus.Completed);
            if (status == MetaLordMoveStatus.Failed)
                Log.Error("[CQF] MetaLord move failed: " + this.name + "/" + move.pawn + "/" + reason);
            if (notify) this.NotifyMoveResult(move);
        }

        private void NotifyMoveResult(MoveRequirment move)
        {
            this.Trigger(new LordEventSignal(move.status == MetaLordMoveStatus.Completed ? LordEventSignalType.MoveCompleted :
                move.status == MetaLordMoveStatus.Cancelled ? LordEventSignalType.MoveCancelled : LordEventSignalType.MoveFailed)
            { pawn = move.pawn, group = move.pawn?.GetLord(), targetMap = move.targetMap, reason = move.reason });
        }

        private void RefreshDuty(Pawn pawn, bool interrupt)
        {
            if (pawn?.GetLord()?.LordJob is MultLevelLordJobBase job && job.metaLord == this)
                pawn.GetLord().CurLordToil?.UpdateAllDuties();
            if (interrupt && pawn?.CurJob != null && !pawn.Downed && !pawn.Drafted)
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        public int loadID;
        [NoTranslate] public string name;
        public Map rootMap;
        public Faction faction;
        public Quest quest;
        public MetaLordEventDef eventDef;
        internal MetaLordManager manager;
        private int nextEventTick;
        private bool disposed;
        private bool processingEvents;
        private int memberRevision;
        private List<Pawn> members = new List<Pawn>();
        private List<LevelLord> levelLords = new List<LevelLord>();
        private List<MoveRequirment> moves = new List<MoveRequirment>();
        private List<MoveRequirment> recentMoves = new List<MoveRequirment>();
        private Dictionary<string, MetaLordEventState> eventStates = new Dictionary<string, MetaLordEventState>();
        private List<string> stateKeys;
        private List<MetaLordEventState> stateValues;
        private readonly Dictionary<Pawn, MoveRequirment> moveIndex = new Dictionary<Pawn, MoveRequirment>();
        private readonly Queue<LordEventSignal> eventQueue = new Queue<LordEventSignal>();
    }
}
