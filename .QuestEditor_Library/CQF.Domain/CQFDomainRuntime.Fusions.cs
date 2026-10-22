using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public bool CanBeginOpening(Pawn pawn) => CanBeginOpening(pawn, 12f);

    public bool CanBeginOpening(Pawn pawn, float openingRadius)
    {
        if (transition == null) return true;
        DomainOpening? owner = transition.OpeningOwner;
        if (owner?.SupportsFusion != true || owner.Fusion != null || !owner.Active || owner.Map != pawn.Map || owner.Caster == pawn) return false;
        float radius = owner.Radius + openingRadius;
        return !pawn.Map.GetComponent<MapComponent_Domain>().HasActiveDomain
            && owner.Position.DistanceToSquared(pawn.Position) <= radius * radius;
    }

    public bool IsOpeningCaster(Pawn pawn)
        => activeOpenings.Any(opening => opening.Active && opening.Caster == pawn)
            || pawn.stances.curStance is Stance_Warmup
                && pawn.CurJob?.ability?.def.comps?.Any(properties => properties is AbilityCompProperties_DomainProtection) == true;

    internal int? FindOpeningStunEnd(Pawn pawn, DomainOpening except)
    {
        int? original = null;
        foreach (DomainOpening opening in activeOpenings)
            if (opening != except && opening.OriginalStunEnd(pawn) is int value)
                original = original == null ? value : Math.Min(original.Value, value);
        return original;
    }

    internal bool HasPendingCounter(DomainOpening owner)
    {
        if (owner.Fusion?.Active == true) return false;
        if (owner.SupportsFusion && !owner.Map.GetComponent<MapComponent_Domain>().HasActiveDomain)
            foreach (DomainOpening other in activeOpenings)
            {
                if (other == owner || !other.Active || !other.SupportsFusion || other.Map != owner.Map || other.Caster == owner.Caster || other.Fusion != null) continue;
                float contactRadius = owner.Radius + other.Radius;
                if (other.Position.DistanceToSquared(owner.Position) <= contactRadius * contactRadius) return true;
            }
        foreach (Pawn pawn in owner.Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == owner.Caster || pawn.Dead || activeOpenings.Any(opening => opening.Active && opening.Caster == pawn)) continue;
            AbilityDef? ability = pawn.CurJob?.ability?.def;
            if (ability == null || pawn.stances.curStance is not Stance_Warmup) continue;
            DomainDef? definition = DefDatabase<DomainDef>.AllDefsListForReading.FirstOrDefault(domain => domain.openingAbility == ability);
            if (definition == null) continue;
            float radius = owner.Radius + definition.OpeningRadius;
            if (pawn.Position.DistanceToSquared(owner.Position) <= radius * radius) return true;
        }
        return false;
    }

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
        if (fusion.ExistingAnchor == null && !HasAvailable(fusion.FirstDefinition)) host.Hibernate(GenerateDomain(fusion.FirstDefinition, fusion.Map));
        GetTerrainTemplate(fusion.Second.Definition, fusion.Map);
    }

    internal void EnterFusion(DomainOpening_Fusion fusion)
    {
        if (fusion.ExistingAnchor != null)
        {
            DomainOpening opening = fusion.Second;
            opening.ReleaseForFusion();
            MapComponent_Domain existing = fusion.ExistingAnchor.Domain
                ?? throw new InvalidOperationException("[CQF] The challenged domain no longer exists.");
            MapComponent_Domain side = existing.EnterChallenger(opening.Definition, opening.Caster!, opening.CaptureEntrants(),
                GetMapTemplate(opening.Definition, fusion.Map), true);
            transition?.BindContest(existing, side);
            return;
        }
        DomainOpening first = fusion.First;
        DomainOpening second = fusion.Second;
        first.ReleaseForFusion();
        second.ReleaseForFusion();
        if (!first.Active || !second.Active || first.Caster?.Spawned != true || second.Caster?.Spawned != true)
            throw new InvalidOperationException("[CQF] Fused domain casters could not be released on the source map.");
        Map source = fusion.Map;
        List<Pawn> firstEntrants = first.CaptureEntrants();
        List<Pawn> secondTargets = second.CaptureEntrants().Where(pawn => pawn != first.Caster && pawn != second.Caster
            && (!firstEntrants.Contains(pawn) || pawn.Position.DistanceToSquared(second.Position) < pawn.Position.DistanceToSquared(first.Position))).ToList();
        List<Pawn> firstTargets = firstEntrants.Where(pawn => pawn != first.Caster && pawn != second.Caster && !secondTargets.Contains(pawn)).ToList();
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
        foreach (DomainOpening opening in candidates)
            foreach (Map realm in domainMaps)
            {
                MapComponent_Domain existing = realm.GetComponent<MapComponent_Domain>();
                Building_Domain? anchor = existing.Anchor;
                if (!existing.CanAcceptChallenger || anchor?.Spawned != true || anchor.Map != opening.Map || anchor.OpeningFusion != null) continue;
                float radius = opening.Radius + existing.Definition.OpeningRadius;
                if (anchor.Position.DistanceToSquared(opening.Position) > radius * radius) continue;
                if (transition != null)
                {
                    if (transition.OpeningOwner != opening) continue;
                    transition.DetachOpening();
                    SetTransition(null);
                }
                openings.Remove(opening);
                DomainOpening_Fusion.Create(anchor, opening);
                return;
            }
#else
        fusionCheckRequested = false;
#endif
    }

    private readonly HashSet<DomainOpening> activeOpenings = new();
    private bool fusionCheckRequested;
}
