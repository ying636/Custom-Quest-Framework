#if RIMWORLD_1_7
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class JobGiver_CQFCrossMapMove : JobGiver_CQFCrossMapTarget
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!pawn.Spawned || GameComponent_MapManager.Instance is not { } manager ||
            ResolveDestination(pawn, manager) is not { } map)
            return null;
        return manager.CrossMap.OrderJob(manager, pawn, map);
    }
}
#endif
