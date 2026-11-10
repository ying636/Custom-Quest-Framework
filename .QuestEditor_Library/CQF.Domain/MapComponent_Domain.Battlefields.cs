using Verse;

namespace QuestEditor_Library;

public sealed partial class MapComponent_Domain
{
    public bool IsBattlefield => Battlefield.battlefieldMap;

    internal void MarkBattlefield() => battlefieldMap = true;

    internal void CopyToBattlefield(MapComponent_Domain destination, IntVec3 offset)
    {
        if (!CanAcceptChallenger || !destination.Available || destination.map == map)
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
        destination.InstallProtection();
    }

    internal void CommitBattlefieldTransfer()
    {
        worker = transferReplacement ?? throw new InvalidOperationException("[CQF] Domain transfer has no prepared replacement.");
        transferReplacement = null;
        transferLayoutMoved = false;
        active = closing = collapsed = arrivalLocked = returnQueued = false;
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
        if (transferLayoutMoved)
        {
            layout?.ParkAll();
            layout?.MoveTo(this, destination.map.Size, new IntVec3(-transferOffset.x, 0, -transferOffset.z));
        }
        transferLayoutMoved = false;
        transferReplacement?.Dispose();
        transferReplacement = null;
        anchor?.BindDomain(this);
        InstallProtection();
        destination.active = destination.closing = destination.arrivalLocked = false;
        destination.worker = null;
        destination.caster = null;
        destination.sourceMap = null;
        destination.anchor = null;
        destination.participants.Clear();
        destination.layout = null;
        destination.health = 0f;
    }

    internal void AttachTransferredContest(MapComponent_Domain side, DomainTerrainSnapshot terrain)
    {
        challenger = side;
        contest = new DomainContest(this, terrain);
        contest.Initialize(this);
        worker!.ContestStarted();
        side.worker!.ContestStarted();
    }

    internal void AbortBattlefieldContest(bool borrowedChallenger)
    {
        List<Exception> errors = new();
        if (!borrowedChallenger && challenger != null)
        {
            MapComponent_Domain side = challenger;
            foreach (Action action in new Action[] { side.EndArrivalTransition,
                () => side.worker?.Dissolved(), () => side.anchor?.RemoveNormally(), () => side.layout?.Clear(),
                side.ReleaseScreenBorder, () => side.worker?.Dispose(), () =>
                {
                    if (side.caster == null) return;
                    foreach (Hediff_DomainProtection protection in side.caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>()
                        .Where(protection => protection.Domain == side).ToList()) side.caster.health.RemoveHediff(protection);
                } })
                try { action(); } catch (Exception error) { errors.Add(error); }
        }
        contest?.Dispose();
        contest = null;
        challenger = null;
        if (errors.Count > 0) throw new AggregateException("[CQF] Could not fully abort battlefield entry.", errors);
    }

    private bool battlefieldMap;
    private DomainWorker? transferReplacement;
    private bool transferLayoutMoved;
    private IntVec3 transferOffset;
}
