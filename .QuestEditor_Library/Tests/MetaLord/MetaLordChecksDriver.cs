using System.Text;
using System.Xml;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CQF.MetaLordChecks;

public sealed class MetaLordChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 360f) throw new TimeoutException("MetaLord checks timed out.");
            if (LongEventHandler.AnyEventNowOrWaiting || Current.Game == null || (stage == 0 && Current.Game.CurrentMap == null)) return;
            root = GenFilePaths.SaveDataFolderPath;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            if (stage == 0)
            {
                File.WriteAllLines(Path.Combine(root, "LanguageErrors.txt"), LanguageDatabase.activeLanguage.loadErrors, Encoding.UTF8);
                home = Current.Game.CurrentMap;
                Map a = Generate(home);
                Map b = Generate(home);
                Map deep = Generate(a);
                Check(LevelPather.GetPathPortal(home, home).Count == 0, "same-map route is empty");
                Check(LevelPather.GetPathPortal(home, deep).Count == 2, "nested downward route");
                Check(LevelPather.GetPathPortal(deep, home).Count == 2, "nested upward route excludes destination exit");
                Check(LevelPather.GetPathPortal(a, b).Count == 2, "sibling route traverses common ancestor");
                Check(LevelPather.GetRoot(deep) == home, "arbitrary depth shares root");
                MetaLordManager manager = home.GetComponent<MapComponent_CQFLord>().Manager;
                Check(ReferenceEquals(manager, deep.GetComponent<MapComponent_CQFLord>().Manager), "game-scoped manager shared across layers");
                MetaLord owner = manager.GetOrCreate("CQF_Check", home, Faction.OfPlayer, DefDatabase<MetaLordEventDef>.GetNamed("CQF_MetaLordCheckEvents"));
                DutyMapDef duty = DefDatabase<DutyMapDef>.GetNamed("CQF_MetaLordCheckDuty");
                Lord rootGroup = owner.CreateGroup(home, "CQF_Home", duty);
                owner.CreateGroup(a, "CQF_A", duty);
                owner.CreateGroup(b, "CQF_B", duty);
                Pawn pawn = home.mapPawns.FreeColonistsSpawned.First(p => !p.Downed);
                pawnID = pawn.ThingID;
                pawn.jobs.StopAll();
                owner.AddMember(pawn, "CQF_Home");
                Check(manager.GetForPawn(pawn) == owner && pawn.GetLord() == rootGroup, "member bound in both ownership layers");
                Pawn second = home.mapPawns.FreeColonistsSpawned.First(p => p != pawn && !p.Downed);
                ((MultLevelLordJobBase)rootGroup.LordJob).capacity = 1;
                Check(!rootGroup.CanAddPawn(second), "native Lord intake enforces group capacity");
                Lord overflow = owner.CreateGroup(home, "CQF_Overflow", duty);
                owner.AddMember(second);
                Check(second.GetLord() == overflow, "capacity excludes full groups from allocation");
                bool duplicateRejected = false;
                try { owner.CreateGroup(home, "CQF_Home"); } catch (InvalidOperationException) { duplicateRejected = true; }
                Check(duplicateRejected && pawn.GetLord() == rootGroup, "duplicate group rejected without changing ownership");
                bool factionRejected = false;
                try { manager.GetOrCreate("CQF_Check", home, Faction.OfAncients); } catch (InvalidOperationException) { factionRejected = true; }
                Check(factionRejected, "organization identity rejects conflicting faction");
                owner.RemoveGroup(overflow);
                Check(second.GetLord() == null && manager.GetForPawn(second) == owner, "group removal keeps organization membership");
                owner.RemoveMember(second);
                Check(manager.GetForPawn(second) == null, "explicit leave releases organization index");
                MetaLord reentrant = manager.GetOrCreate("CQF_ReentrantMoves", home, Faction.OfPlayer,
                    DefDatabase<MetaLordEventDef>.GetNamed("CQF_MetaLordCheckRedirectEvents"));
                reentrant.CreateGroup(home, "CQF_ReentrantGroup");
                reentrant.AddMember(second);
                reentrant.RequestMove(second, a);
                MoveRequirment superseded = reentrant.RequestMove(second, b);
                Check(reentrant.Moves.Count == 1 && reentrant.GetMove(second)?.targetMap == a &&
                    superseded.status == MetaLordMoveStatus.Cancelled, "cancellation callback can replace new request without duplicate moves");
                reentrant.eventDef = DefDatabase<MetaLordEventDef>.GetNamed("CQF_MetaLordCheckRemovalEvents");
                reentrant.RemoveMember(second);
                Check(reentrant.Moves.Count == 0 && manager.GetForPawn(second) == null && RequestMoveAction.Skipped == 1,
                    "leave callbacks observe detached membership and cannot recreate pending move");
                manager.Remove(reentrant);
                MetaLord assignment = manager.GetOrCreate("CQF_Assignment", home, Faction.OfPlayer);
                assignment.CreateGroup(home, "CQF_CallbackSource");
                assignment.AddMember(second);
                assignment.CreateGroup(home, "CQF_CallbackReceiver");
                assignment.eventDef = DefDatabase<MetaLordEventDef>.GetNamed("CQF_MetaLordCheckAssignmentEvents");
                MoveRequirment redirectedArrival = assignment.RequestMove(second, home, "CQF_CallbackReceiver");
                Check(redirectedArrival.status == MetaLordMoveStatus.Cancelled && assignment.Moves.Count == 0 &&
                    assignment.RecentMoves.Last().status == MetaLordMoveStatus.Completed,
                    "assignment callback replacement preserves terminal state of superseded arrival");
                manager.Remove(assignment);
                MetaLord joinDisband = manager.GetOrCreate("CQF_JoinDisband", home, Faction.OfPlayer,
                    DefDatabase<MetaLordEventDef>.GetNamed("CQF_MetaLordCheckJoinDisbandEvents"));
                joinDisband.CreateGroup(home, "CQF_JoinDisbandGroup");
                joinDisband.AddMember(second);
                Check(joinDisband.Disposed && manager.GetForPawn(second) == null && second.GetLord() == null && second.mindState.duty == null,
                    "join callback can disband without restoring removed Lord or stale duty");
                ((MultLevelLordJobBase)rootGroup.LordJob).capacity = 0;
                CQFAction_MetaLord create = new CQFAction_MetaLord
                {
                    operation = MetaLordOperation.CreateGroup, organizationName = "CQF_Check", groupName = "CQF_Action",
                    dutyMap = duty, eventDef = owner.eventDef, capacity = 2, groupTags = new List<string> { "CQF_Tag" }
                };
                CQFAction_MetaLord copied = (CQFAction_MetaLord)create.Copy();
                copied.Work(new Dictionary<string, TargetInfo> { ["Pawn"] = pawn, ["TargetMap"] = new TargetInfo(pawn.Position, home) }, null);
                Lord actionGroup = owner.Levels.Single(l => l.level == home).Lords.Single(l => ((MultLevelLordJobBase)l.LordJob).groupName == "CQF_Action");
                Check(((MultLevelLordJobBase)actionGroup.LordJob).capacity == 2 && ((MultLevelLordJobBase)actionGroup.LordJob).groupTags.Contains("CQF_Tag"),
                    "CQFAction XML copy resolves Def references and executes configuration");
                owner.RemoveGroup(actionGroup);
                LordData data = new LordData
                {
                    name = "CQF_FromData", metaLordName = "CQF_Check", faction = "MapFaction", metaLordEvents = owner.eventDef,
                    capacity = 3, groupTags = new List<string> { "CQF_Tag" },
                    lordJobData = new LordJobData { lordJob = typeof(LordJob_MetaLord), dutyMap = duty }
                };
                XmlDocument document = new XmlDocument(); document.LoadXml(data.SaveToXElement("li").ToString());
                LordData loadedData = DirectXmlToObject.ObjectFromXml<LordData>(document.DocumentElement, false);
                DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
                LordJob configured = loadedData.Data.CreateJob(home, null);
                Lord configuredGroup = LordMaker.MakeNewLord(owner.faction, configured, home);
                Check(configured is MultLevelLordJobBase configuredJob && configuredJob.metaLord == owner && configuredJob.defaultDutyMap == duty &&
                    configuredJob.capacity == 3 && configuredJob.groupTags.Contains("CQF_Tag"), "LordData XML factory binds multi-level job and default DutyMap");
                owner.RemoveGroup(configuredGroup);
                GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, duty, null);
                Check(pawn.GetLord() == rootGroup && rootGroup.LordJob is MultLevelLordJobBase, "DutyMap does not replace group job");
                MoveRequirment cancelled = owner.RequestMove(pawn, a, "CQF_A");
                Check(pawn.mindState.duty.def == QEDefOf.QE_Duty_MoveLevel, "move duty immediately overrides personal duty");
                ((LordJob_ComplexCustom)rootGroup.LordJob).ApplyDuty(pawn);
                Check(pawn.mindState.duty.def == QEDefOf.QE_Duty_MoveLevel, "DutyMap refresh preserves movement priority");
                owner.CancelMove(pawn);
                Check(cancelled.status == MetaLordMoveStatus.Cancelled && pawn.mindState.duty.def == DutyDefOf.Defend, "cancel restores DutyMap");
                MoveRequirment replaced = owner.RequestMove(pawn, a);
                owner.RequestMove(pawn, b);
                Check(replaced.status == MetaLordMoveStatus.Cancelled && owner.Moves.Count == 1, "replacement leaves one request per pawn");
                owner.CancelMove(pawn);
                Signal("CQF_Once"); Signal("CQF_Once");
                Check(CounterAction.Counts["CQF_Once"] == 1, "single-use event runs once");
                Signal("CQF_Cooldown"); Signal("CQF_Cooldown");
                Check(CounterAction.Counts["CQF_Cooldown"] == 1, "event cooldown blocks immediate repeats");
                Signal("CQF_Reentrant");
                Check(CounterAction.Counts["CQF_Reentrant"] == 1, "nested signal dispatch handles reentrancy");
                Permit(pawn, a); Permit(pawn, b); Permit(pawn, deep);
                MoveRequirment blocked = owner.RequestMove(pawn, a, "CQF_A");
                ((MapParent_Custom)a.Parent).entrance.opended = false;
                Check(!owner.TryGetNextPortal(pawn, out _) && blocked.status == MetaLordMoveStatus.Waiting, "closed entrance waits without losing request");
                ((MapParent_Custom)a.Parent).entrance.opended = true;
                owner.CancelMove(pawn);
                RunTicks(180);
                Check(CounterAction.Counts["CQF_Cooldown"] == 1 && CounterAction.Counts["CQF_Tick"] > 0, "tick events execute independently");
                Signal("CQF_Cooldown");
                Check(CounterAction.Counts["CQF_Cooldown"] == 2, "cooldown expires");
                MoveWithJob(owner, pawn, a, "CQF_A", false);
                Check(pawn.GetLord()?.Map == a && manager.GetForPawn(pawn) == owner, "custom movement job retains target Lord");
                RunTicks(1250);
                MoveWithJob(owner, pawn, home, "CQF_Home", false);
                Check(pawn.GetLord() == rootGroup, "upward movement restores root group");
                RunTicks(1250);
                MoveWithJob(owner, pawn, a, "CQF_A", true);
                Check(pawn.GetLord()?.Map == a, "vanilla EnterPortal exit notification preserves new Lord");
                RunTicks(1250);
                owner.RequestMove(pawn, b, "CQF_B");
                while (pawn.Map != b)
                {
                    Check(owner.TryGetNextPortal(pawn, out CQFMapPortal portal), "sibling hop available");
                    StartAndWait(pawn, portal, false);
                    RunTicks(1250);
                }
                Check(owner.GetMove(pawn) == null && pawn.GetLord()?.Map == b, "sibling transfer completes after intermediate attachment");
                owner.RequestMove(pawn, home, "CQF_Missing");
                ((MapParent_Custom)b.Parent).Exit.Exit(pawn);
                Check(pawn.GetLord()?.Map == home && owner.GetMove(pawn)?.status == MetaLordMoveStatus.Waiting,
                    "rejected final group retains local ownership and request");
                owner.CancelMove(pawn);
                owner.RequestMove(pawn, deep);
                while (pawn.Map != deep)
                {
                    if (!owner.TryGetNextPortal(pawn, out CQFMapPortal portal))
                    {
                        if (owner.GetMove(pawn) == null) throw new InvalidOperationException("Nested downward request failed.");
                        RunTicks(120);
                        continue;
                    }
                    Check(portal.Map == pawn.Map, "nested downward hop selected on current layer");
                    StartAndWait(pawn, portal, false);
                    RunTicks(1250);
                }
                Check(owner.GetMove(pawn) == null && pawn.GetLord()?.Map == deep, "unconfigured deep layer creates group and retains personal DutyMap");
                owner.RequestMove(pawn, home, "CQF_Home");
                while (pawn.Map != home)
                {
                    if (!owner.TryGetNextPortal(pawn, out CQFMapPortal portal))
                    {
                        if (owner.GetMove(pawn) == null) throw new InvalidOperationException("Nested upward request failed.");
                        RunTicks(120);
                        continue;
                    }
                    Check(portal.Map == pawn.Map, "nested upward hop selected on current layer");
                    StartAndWait(pawn, portal, false);
                    RunTicks(1250);
                }
                Check(pawn.GetLord() == rootGroup && owner.GetMove(pawn) == null, "nested round trip completes through intermediate layers");
                MoveRequirment timeout = owner.RequestMove(pawn, a, "CQF_A", 60);
                ((MapParent_Custom)a.Parent).entrance.opended = false;
                RunTicks(120);
                Check(timeout.status == MetaLordMoveStatus.Failed && owner.GetMove(pawn) == null, "expired request reports failure and restores duties");
                ((MapParent_Custom)a.Parent).entrance.opended = true;
                owner.RequestMove(pawn, a, "CQF_A");
                homeID = home.uniqueID;
                targetID = a.uniqueID;
                GameComponent_MapHibernation.Instance.Hibernate(home);
                int before = CounterAction.Counts["CQF_Tick"];
                int requestTick = owner.GetMove(pawn)!.createdTick;
                RunTicks(120);
                Check(CounterAction.Counts["CQF_Tick"] > before, "root hibernation does not stop organization coordinator");
                Check(owner.GetMove(pawn)!.createdTick == requestTick + 120, "source hibernation pauses movement timeout");
                GameDataSaveLoader.SaveGame("CQF_MetaLordChecks");
                Current.Game.Dispose();
                SavedGameLoaderNow.LoadGameFromSaveFileNow("CQF_MetaLordChecks");
                stage = 1;
                return;
            }
            if (stage == 1)
            {
                GameComponent_MapHibernation hibernation = GameComponent_MapHibernation.Instance;
                home = hibernation.HibernatingMaps.Single(m => m.uniqueID == homeID);
                MetaLordManager manager = home.GetComponent<MapComponent_CQFLord>().Manager;
                MetaLord owner = manager.Get("CQF_Check", home);
                Pawn pawn = owner.Members.Single(p => p.ThingID == pawnID);
                Map target = Current.Game.Maps.Single(m => m.uniqueID == targetID);
                Check(owner.GetMove(pawn)?.targetMap == target && manager.GetForPawn(pawn) == owner, "load rebuilds member and pending move indexes");
                Check(pawn.GetLord()?.LordJob is MultLevelLordJobBase job && job.metaLord == owner, "load restores group reverse reference");
                Signal("CQF_Once");
                Check(CounterAction.Counts["CQF_Once"] == 1, "single-use event state survives save/load");
                hibernation.Activate(home);
                Permit(pawn, target);
                ((MapParent_Custom)target.Parent).entrance.CD.Clear();
                MoveWithJob(owner, pawn, target, "CQF_A", false);
                Check(pawn.GetLord()?.Map == target, "loaded move can complete");
                RunTicks(1250);
                MoveWithJob(owner, pawn, home, "CQF_Home", false);
                owner.RequestMove(pawn, target, "CQF_A");
                PocketMapUtility.DestroyPocketMap(target);
                Check(owner.GetMove(pawn) == null && owner.RecentMoves.Last().status == MetaLordMoveStatus.Failed,
                    "target destruction fails requests");
                Check(owner.Levels.All(l => l.level != target), "target destruction removes layer records");
                Lord source = pawn.GetLord();
                manager.Remove(owner);
                Check(manager.GetForPawn(pawn) == null && pawn.GetLord() == null && !home.lordManager.lords.Contains(source),
                    "disband removes real Lords and member ownership");
                MetaLord disposed = manager.GetOrCreate("CQF_Dispose", home, Faction.OfPlayer);
                disposed.CreateGroup(home, "CQF_DisposeGroup");
                disposed.AddMember(pawn);
                disposed.Dispose();
                Check(manager.Get("CQF_Dispose", home) == null && manager.GetForPawn(pawn) == null && pawn.GetLord() == null,
                    "direct disposal unregisters organization and releases members");
                Complete(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(root, "MetaLordChecks.txt"), "FAIL stage " + stage + "\n" + error + "\n", Encoding.UTF8);
            Log.Error("[CQF] MetaLord checks failed: " + error);
            Complete(false);
        }
    }

    private Map Generate(Map source)
    {
        MapParent_Custom parent = (MapParent_Custom)WorldObjectMaker.MakeWorldObject(QEDefOf.QE_CustomMap_SubMap);
        parent.permanent = true;
        parent.SetFaction(Faction.OfPlayer);
        Map result = GameTools.GenerateSubMap(new IntVec3(40, 1, 40), parent, parent.def.mapGenerator, Enumerable.Empty<GenStepWithParams>(), source);
        IntVec3 sourceRoot = source.Parent is MapParent_Custom sourceParent ? sourceParent.Exit.Position : source.mapPawns.FreeColonistsSpawned.First().Position;
        IntVec3 entranceCell = CellFinder.RandomClosewalkCellNear(sourceRoot, source, 8, cell => cell.GetFirstThing<CQFMapPortal>(source) == null);
        IntVec3 exitCell = CellFinder.StandableCellNear(result.Center, result, 10);
        CustomMapEntrance entrance = (CustomMapEntrance)GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("QE_CustomMapEntrance")), entranceCell, source);
        CustomMapExit exit = (CustomMapExit)GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("QE_CustomMapExit")), exitCell, result);
        entrance.CustomMap = result; entrance.exit = exit; exit.entrance = entrance;
        parent.entrance = entrance; parent.exit = exit;
        return result;
    }

    private void Permit(Pawn pawn, Map target)
    {
        foreach (Map map in LevelPather.GetAllParentMaps(target))
            if (map.Parent is MapParent_Custom parent && !GameComponent_LevelSchedule.Instance.GetSchedule(pawn).allowedLevels.Contains(parent.entrance))
                GameComponent_LevelSchedule.Instance.GetSchedule(pawn).allowedLevels.Add(parent.entrance);
    }

    private void MoveWithJob(MetaLord owner, Pawn pawn, Map target, string group, bool vanilla)
    {
        owner.RequestMove(pawn, target, group);
        Check(owner.TryGetNextPortal(pawn, out CQFMapPortal portal), "next movement portal selected: " + owner.GetMove(pawn)?.reason);
        StartAndWait(pawn, portal, vanilla);
        Check(pawn.Map == target && owner.GetMove(pawn) == null, "movement request completed");
    }

    private void StartAndWait(Pawn pawn, CQFMapPortal portal, bool vanilla)
    {
        Map before = pawn.Map;
        pawn.jobs.StartJob(JobMaker.MakeJob(vanilla ? JobDefOf.EnterPortal : QEDefOf.CQF_MetaLordMove, portal), JobCondition.InterruptForced);
        for (int i = 0; i < 6000 && pawn.Map == before; i++) Find.TickManager.DoSingleTick();
        Check(pawn.Map != before, "portal job actually traverses map");
    }

    private static void RunTicks(int count) { for (int i = 0; i < count; i++) Find.TickManager.DoSingleTick(); }
    private static void Signal(string tag) { Find.SignalManager.SendSignal(new Signal(tag)); }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        File.AppendAllText(Path.Combine(root, "MetaLordChecks.txt"), "PASS " + description + "\n", Encoding.UTF8);
    }

    private void Complete(bool success)
    {
        finished = true;
        File.AppendAllText(Path.Combine(root, "MetaLordChecks.txt"), success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        Application.Quit(success ? 0 : 1);
    }

    private string root = GenFilePaths.SaveDataFolderPath;
    private int stage;
    private bool finished;
    private Map home = null!;
    private int homeID;
    private int targetID;
    private string pawnID = "";
}
