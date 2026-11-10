using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    internal void NotifyDomainEntered(Map map)
    {
        failedContestPairs.RemoveWhere(pair => pair.first == map || pair.second == map);
        contestCheckRequested = true;
        fusionCheckRequested = true;
    }

    public MapComponent_Domain EnterOrContest(DomainDef definition, Pawn caster, IEnumerable<Pawn> targets)
    {
        MapComponent_Domain current = caster.Map.GetComponent<MapComponent_Domain>();
        if (current.HasActiveDomain)
        {
            if (!current.CanAcceptChallenger) throw new InvalidOperationException("[CQF] This domain cannot accept another challenger.");
            return EnterBattlefield(current, definition, caster, targets, false);
        }
        Map realm = Acquire(definition, caster);
        MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
        domain.Enter(caster, targets);
        return domain;
    }

    public MapComponent_Domain MergeDomains(MapComponent_Domain first, MapComponent_Domain second)
    {
        if (!first.CanAcceptChallenger || !second.CanAcceptChallenger || first.map == second.map
            || !domainMaps.Contains(first.map) || !domainMaps.Contains(second.map))
            throw new InvalidOperationException("[CQF] Only two registered, ready, contest-capable domains can merge.");
        FinishTransitionFor(first.map);
        FinishTransitionFor(second.map);
        return TransferBattlefield(first, second, second.Definition,
            _ => throw new InvalidOperationException("[CQF] A transferred domain pair requires both instances."));
    }

    private void ProcessDomainContests()
    {
        if (!contestCheckRequested || transition != null) return;
        contestCheckRequested = false;
        if (domainMaps.Count < 2) return;
        for (int firstIndex = 0; firstIndex < domainMaps.Count; firstIndex++)
        {
            MapComponent_Domain first = domainMaps[firstIndex].GetComponent<MapComponent_Domain>();
            if (!first.CanAcceptChallenger || first.Anchor?.Spawned != true) continue;
            for (int secondIndex = firstIndex + 1; secondIndex < domainMaps.Count; secondIndex++)
            {
                MapComponent_Domain second = domainMaps[secondIndex].GetComponent<MapComponent_Domain>();
                if (failedContestPairs.Contains((first.map, second.map))) continue;
                if (!second.CanAcceptChallenger || second.Anchor?.Spawned != true || first.SourceMap != second.SourceMap) continue;
                float radius = first.Definition.OpeningRadius + second.Definition.OpeningRadius;
                if (first.Anchor.Position.DistanceToSquared(second.Anchor.Position) > radius * radius) continue;
                try { MergeDomains(first, second); }
                catch (Exception error)
                {
                    Log.Error("[CQF] Could not merge overlapping domains; automatic retry is disabled for this pair: " + error);
                    failedContestPairs.Add((first.map, second.map));
                }
                contestCheckRequested = true;
                return;
            }
        }
    }

    private readonly HashSet<(Map first, Map second)> failedContestPairs = new();
    private bool contestCheckRequested;
}
