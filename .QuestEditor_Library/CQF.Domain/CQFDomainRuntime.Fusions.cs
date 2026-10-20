using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public bool CanBeginOpening(Pawn pawn)
    {
        if (transition == null) return true;
        DomainOpening? owner = transition.OpeningOwner;
        if (owner?.SupportsFusion != true || owner.Fusion != null || !owner.Active || owner.Map != pawn.Map || owner.Caster == pawn) return false;
        float radius = owner.Radius + 12f;
        return !pawn.Map.GetComponent<MapComponent_Domain>().HasActiveDomain
            && owner.Position.DistanceToSquared(pawn.Position) <= radius * radius;
    }

    public bool IsOpeningCaster(Pawn pawn)
        => activeOpenings.Any(opening => opening.Active && opening.Caster == pawn)
            || pawn.CurJob?.ability?.def.comps?.Any(properties => properties is AbilityCompProperties_DomainProtection) == true;

    internal void RegisterOpening(DomainOpening opening)
    {
        activeOpenings.Add(opening);
        fusionCheckRequested = true;
    }

    internal void UnregisterOpening(DomainOpening opening)
    {
        activeOpenings.Remove(opening);
        openings.Remove(opening);
        fusionCheckRequested = true;
    }

    internal void PrepareFusion(DomainOpening_Fusion fusion)
    {
        if (!fusion.Active) return;
        if (!HasAvailable(fusion.First.Definition)) host.Hibernate(GenerateDomain(fusion.First.Definition, fusion.Map));
        GetTerrainTemplate(fusion.Second.Definition, fusion.Map);
    }

    internal void EnterFusion(DomainOpening_Fusion fusion)
    {
        DomainOpening first = fusion.First;
        DomainOpening second = fusion.Second;
        first.ReleaseForFusion();
        second.ReleaseForFusion();
        if (!first.Active || !second.Active || first.Caster?.Spawned != true || second.Caster?.Spawned != true)
            throw new InvalidOperationException("[CQF] Fused domain casters could not be released on the source map.");
        Map source = fusion.Map;
        List<Pawn> secondTargets = second.Entrants.Where(pawn => pawn != first.Caster && pawn != second.Caster && pawn.Spawned && !pawn.Dead && pawn.Map == source
            && (!first.Entrants.Contains(pawn) || pawn.Position.DistanceToSquared(second.Position) < pawn.Position.DistanceToSquared(first.Position))).Distinct().ToList();
        List<Pawn> firstTargets = first.Entrants.Where(pawn => pawn != first.Caster && pawn != second.Caster && pawn.Spawned && !pawn.Dead && pawn.Map == source
            && !secondTargets.Contains(pawn)).Distinct().ToList();
        DomainMapTemplate template = GetMapTemplate(second.Definition, source);
        MapComponent_Domain? primary = null;
        try
        {
            primary = Acquire(first.Definition, first.Caster).GetComponent<MapComponent_Domain>();
            primary.Enter(first.Caster, firstTargets);
            MapComponent_Domain challenger = primary.EnterChallenger(second.Definition, second.Caster, secondTargets, template, true);
            transition?.BindContest(primary, challenger);
        }
        catch
        {
            if (primary?.Active == true) primary.RequestReturn("fused domain entry failed");
            throw;
        }
    }

    private void ProcessOpeningFusions()
    {
        if (!fusionCheckRequested) return;
#if RIMWORLD_1_7
        if (transition != null && transition.OpeningOwner?.SupportsFusion != true) return;
        fusionCheckRequested = false;
        DomainOpening[] candidates = activeOpenings.Where(opening => opening.SupportsFusion && opening.Active && opening.Fusion == null
            && !opening.Map.GetComponent<MapComponent_Domain>().HasActiveDomain).ToArray();
        for (int firstIndex = 0; firstIndex < candidates.Length; firstIndex++)
            for (int secondIndex = firstIndex + 1; secondIndex < candidates.Length; secondIndex++)
            {
                DomainOpening first = candidates[firstIndex];
                DomainOpening second = candidates[secondIndex];
                if (first.Map != second.Map || first.Caster == second.Caster) continue;
                float radius = first.Radius + second.Radius;
                if (first.Position.DistanceToSquared(second.Position) > radius * radius) continue;
                if (transition != null)
                {
                    if (transition.OpeningOwner != first && transition.OpeningOwner != second) continue;
                    transition.DetachOpening();
                    SetTransition(null);
                }
                openings.Remove(first);
                openings.Remove(second);
                DomainOpening_Fusion.Create(first, second);
                return;
            }
#else
        fusionCheckRequested = false;
#endif
    }

    private readonly HashSet<DomainOpening> activeOpenings = new();
    private bool fusionCheckRequested;
}
