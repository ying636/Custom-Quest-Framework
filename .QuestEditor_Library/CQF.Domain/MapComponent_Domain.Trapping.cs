using Verse;

namespace QuestEditor_Library;

public partial class MapComponent_Domain
{
    public bool Trapped => trapped;

    private MapComponent_Domain? CaptiveDomain => Battlefield.trapped ? Battlefield
        : Battlefield.Challenger?.trapped == true ? Battlefield.Challenger : null;

    private Map? ResolveReturnMap() => sourceMap != null && !sourceMap.Disposed && Find.Maps.Contains(sourceMap) ? sourceMap
        : Find.Maps.FirstOrDefault(candidate => candidate != map && candidate.IsPlayerHome);

    private void HoldInBattlefield()
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
        MapComponent_Domain survivor = ReferenceEquals(this, Battlefield) ? Battlefield.Challenger! : Battlefield;
        survivor.Worker?.TerritoryChanged();
        Log.Message("[CQF] Domain " + Definition.defName + " dissolved; occupants remain inside " + survivor.Definition.defName + ".");
    }

    private bool trapped;
}
