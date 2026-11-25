#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class JobGiver_CQFCrossMapReport : JobGiver_CQFCrossMapTarget
{
    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var copy = (JobGiver_CQFCrossMapReport)base.DeepCopy(resolve);
        copy.detectionRadius = detectionRadius;
        copy.reinforcementGroupTag = reinforcementGroupTag;
        copy.responders = responders;
        copy.assemblyTicks = assemblyTicks;
        copy.cooldownTicks = cooldownTicks;
        return copy;
    }

    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!pawn.Spawned || pawn.Downed || string.IsNullOrWhiteSpace(reinforcementGroupTag) ||
            GameComponent_MapManager.Instance is not { } manager ||
            ResolveDestination(pawn, manager) is not { } map || map == pawn.Map)
            return null;
        float radius = Mathf.Clamp(detectionRadius, 1f, 60f);
        foreach (Pawn colonist in pawn.Map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.HostileTo(colonist) && pawn.Position.DistanceToSquared(colonist.Position) <= radius * radius &&
                GenSight.LineOfSight(pawn.Position, colonist.Position, pawn.Map))
                return manager.CrossMap.ReportJob(manager, pawn, map, reinforcementGroupTag, responders, assemblyTicks, cooldownTicks);
        }
        return null;
    }

    public float detectionRadius = 20f;
    [NoTranslate]
    public string reinforcementGroupTag = "";
    public int responders = 4;
    public int assemblyTicks = 180;
    public int cooldownTicks = 2500;
}
#endif
