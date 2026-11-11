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
            || first.InstanceCount + second.InstanceCount > MapComponent_Domain.MaximumDomains
            || !domainMaps.Contains(first.map) || !domainMaps.Contains(second.map))
            throw new InvalidOperationException("[CQF] Only two registered, ready, contest-capable domains can merge.");
        if (transition?.CommittingTransfer != true)
        {
            FinishTransitionFor(first.map);
            FinishTransitionFor(second.map);
        }
        return TransferDomainGroups(first, second, null, null);
    }

    private void ProcessDomainContests()
    {
        if (!contestCheckRequested || transition != null) return;
        contestCheckRequested = false;
        if (domainMaps.Count < 2) return;
        for (int firstIndex = 0; firstIndex < domainMaps.Count; firstIndex++)
        {
            MapComponent_Domain first = domainMaps[firstIndex].GetComponent<MapComponent_Domain>();
            if (!first.CanAcceptChallenger) continue;
            for (int secondIndex = firstIndex + 1; secondIndex < domainMaps.Count; secondIndex++)
            {
                MapComponent_Domain second = domainMaps[secondIndex].GetComponent<MapComponent_Domain>();
                if (failedContestPairs.Contains((first.map, second.map))) continue;
                if (!second.CanAcceptChallenger || first.SourceMap != second.SourceMap
                    || first.InstanceCount + second.InstanceCount > MapComponent_Domain.MaximumDomains) continue;
                var contact = first.Instances.Where(side => side.Active && side.Anchor?.Spawned == true)
                    .SelectMany(left => second.Instances.Where(right => right.Active && right.Anchor?.Spawned == true
                        && right.Anchor.Map == left.Anchor!.Map).Select(right => (left, right)))
                    .FirstOrDefault(pair => pair.left.Anchor!.Position.DistanceToSquared(pair.right.Anchor!.Position)
                        <= (pair.left.Definition.OpeningRadius + pair.right.Definition.OpeningRadius)
                            * (pair.left.Definition.OpeningRadius + pair.right.Definition.OpeningRadius));
                if (contact.left == null) continue;
                try
                {
#if RIMWORLD_1_7
                    Map? visible = Find.CurrentMap;
                    if (visible == first.map || visible == second.map || visible == first.SourceMap)
                    {
                        var plan = PlanBattlefield(first, second, null, null);
                        if (!HasSizedBattlefield(plan.size))
                        {
                            LongEventHandler.QueueLongEvent(() =>
                            {
                                try { PrepareSizedBattlefield(plan.size, first.Definition, first.SourceMap ?? first.map); }
                                catch (Exception error)
                                { failedContestPairs.Add((first.map, second.map)); Log.Error("[CQF] Could not prepare a growing battlefield: " + error); }
                                finally { contestCheckRequested = true; }
                            }, "GeneratingMap", false, null);
                            return;
                        }
                        MapComponent_Domain incoming = contact.right;
                        IntVec3 origin = visible == first.map ? first.map.Center + incoming.SourceOrigin - first.ProjectionOrigin
                            : visible == second.map ? incoming.Caster!.Position : incoming.Anchor!.Position;
                        DomainCounterTransition effect = new(incoming.Definition, origin);
                        SetTransition(effect);
                        effect.BindTransfer(first, second, visible!, () =>
                        {
                            try
                            {
                                MapComponent_Domain side = MergeDomains(first, second);
                                effect.BindContest(side.Battlefield, side);
                            }
                            catch (Exception error)
                            {
                                failedContestPairs.Add((first.map, second.map)); SetTransition(null);
                                Log.Error("[CQF] Could not merge domain groups under fullscreen cover: " + error);
                            }
                        });
                    }
                    else
#endif
                        MergeDomains(first, second);
                }
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
