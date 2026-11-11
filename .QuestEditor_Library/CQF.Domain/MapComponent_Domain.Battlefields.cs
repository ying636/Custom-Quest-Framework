using Verse;

namespace QuestEditor_Library;

public sealed partial class MapComponent_Domain
{
    public bool IsBattlefield => Battlefield.battlefieldMap;
    internal IntVec3 ProjectionOrigin => Battlefield.battlefieldOrigin.IsValid ? Battlefield.battlefieldOrigin : Battlefield.SourceOrigin;

    internal void MarkBattlefield() => battlefieldMap = true;

    internal void CopyToBattlefield(MapComponent_Domain destination, IntVec3 offset)
    {
        if ((!active && !trapped) || closing || arrivalLocked || !destination.Available || destination.map == map)
            throw new InvalidOperationException("[CQF] Invalid domain battlefield transfer.");
        transferReplacement = Activator.CreateInstance(Definition.workerClass) as DomainWorker
            ?? throw new InvalidOperationException("[CQF] Could not prepare a transferred domain worker.");
        transferReplacement.Domain = this;
        transferOffset = offset;
        ReleaseScreenBorder();
        destination.worker?.Dispose();
        destination.definition = definition;
        destination.worker = worker;
        destination.active = active;
        destination.trapped = trapped;
        destination.health = health;
        destination.caster = caster;
        destination.sourceMap = sourceMap;
        destination.sourceOrigin = sourceOrigin;
        destination.anchor = anchor;
        destination.participants = participants.ToList();
        destination.layout = layout;
        destination.worker!.Domain = destination;
        destination.layout?.MoveTo(destination, map.Size, offset);
        transferLayoutMoved = true;
        destination.anchor?.BindDomain(destination);
        if (destination.active) destination.InstallProtection();
        destination.worker.MapTransferred(map, destination.map.Center - map.Center + offset);
        transferWorkerMoved = true;
    }

    internal void CommitBattlefieldTransfer()
    {
        worker = transferReplacement ?? throw new InvalidOperationException("[CQF] Domain transfer has no prepared replacement.");
        transferReplacement = null;
        transferLayoutMoved = false;
        transferWorkerMoved = false;
        active = closing = collapsed = arrivalLocked = returnQueued = trapped = false;
        caster = null;
        sourceMap = null;
        sourceOrigin = IntVec3.Invalid;
        anchor = null;
        participants = new();
        layout = null;
        health = 0f;
    }

    internal void RestoreBattlefieldTransfer(MapComponent_Domain destination)
    {
        worker!.Domain = this;
        if (transferWorkerMoved) worker.MapTransferred(destination.map, map.Center - destination.map.Center - transferOffset);
        transferWorkerMoved = false;
        if (transferLayoutMoved)
        {
            layout?.ParkAll();
            layout?.MoveTo(this, destination.map.Size, new IntVec3(-transferOffset.x, 0, -transferOffset.z));
        }
        transferLayoutMoved = false;
        transferReplacement?.Dispose();
        transferReplacement = null;
        anchor?.BindDomain(this);
        if (active) InstallProtection();
        destination.active = destination.closing = destination.arrivalLocked = destination.trapped = false;
        destination.worker = null;
        destination.caster = null;
        destination.sourceMap = null;
        destination.anchor = null;
        destination.participants.Clear();
        destination.layout = null;
        destination.health = 0f;
    }

    internal void SetProjectionOrigin(IntVec3 origin) => battlefieldOrigin = origin;

    internal void InitializeTransferredScene(IReadOnlyList<DomainTerrainSnapshot> terrains, IReadOnlyList<IntVec3> centers)
    {
        if (InstanceCount < 2) return;
        contest = new DomainContest(this, terrains[1]);
        contest.InitializeScene(this, terrains, centers);
    }

    internal void DetachTransferredScene()
    {
        contest?.Dispose(); contest = null;
        challenger = null; additionalDomains.Clear();
        battlefieldOrigin = IntVec3.Invalid;
    }

    internal void AbortNewBattlefieldSide()
    {
        List<Exception> failures = new();
        foreach (Action action in new Action[] { EndArrivalTransition, () => worker?.Dissolved(), () => anchor?.RemoveNormally(),
            () => layout?.Clear(), ReleaseScreenBorder, () =>
            {
                if (caster == null) return;
                foreach (Hediff_DomainProtection protection in caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>()
                    .Where(protection => protection.Domain == this).ToList()) caster.health.RemoveHediff(protection);
            }, () => worker?.Dispose() })
            try { action(); } catch (Exception error) { failures.Add(error); }
        anchor = null; layout = null;
        active = closing = trapped = false;
        participants.Clear(); caster = null; worker = null;
        if (failures.Count > 0) throw new AggregateException("[CQF] Could not fully abort a new battlefield side.", failures);
    }

    private bool battlefieldMap;
    private IntVec3 battlefieldOrigin = IntVec3.Invalid;
    private DomainWorker? transferReplacement;
    private bool transferLayoutMoved;
    private bool transferWorkerMoved;
    private IntVec3 transferOffset;
}
