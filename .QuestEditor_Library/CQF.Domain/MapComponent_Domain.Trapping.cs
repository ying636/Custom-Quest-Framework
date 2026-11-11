using Verse;

namespace QuestEditor_Library;

public partial class MapComponent_Domain
{
    public bool Trapped => trapped;

    private MapComponent_Domain? CaptiveDomain => Instances.FirstOrDefault(side => side.trapped);

    private Map? ResolveReturnMap() => sourceMap != null && !sourceMap.Disposed && Find.Maps.Contains(sourceMap) ? sourceMap
        : Find.Maps.FirstOrDefault(candidate => candidate != map && candidate.IsPlayerHome);

    private void HoldInBattlefield(bool visible)
    {
        DomainWorker endedWorker = worker ?? throw new InvalidOperationException("[CQF] Dissolving domain has no worker.");
        EndArrivalTransition();
        endedWorker.PreparingContest();
        layout?.ReleaseOccupants();
        endedWorker.Dissolved();
        if (caster != null)
            foreach (Hediff_DomainProtection protection in caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>()
                .Where(protection => protection.Domain == this).ToList()) caster.health.RemoveHediff(protection);
        anchor?.RemoveNormally();
        anchor = null;
        layout?.Clear();
        layout = null;
        ReleaseScreenBorder();
        active = false;
        closing = false;
        trapped = true;
        health = 0f;
        Contest!.ResolveSurvivor(Battlefield);
        endedWorker.TerritoryChanged();
        MapComponent_Domain survivor = Instances.First(side => side.Active);
        foreach (MapComponent_Domain side in Instances.Where(side => side.Active)) side.Worker?.TerritoryChanged();
        if (visible && CQFDomainRuntime.Instance.Transition is { IsReturning: true } transition
            && (transition.ReturningDomain == null || ReferenceEquals(transition.ReturningDomain, this)) && !transition.Finished)
            transition.BindReturn(map, caster?.PositionHeld.IsValid == true ? caster.PositionHeld : map.Center, map.mapPawns.AllPawnsSpawned);
        Log.Message("[CQF] Domain " + Definition.defName + " dissolved; occupants remain inside " + survivor.Definition.defName + ".");
    }

    private bool trapped;
}
