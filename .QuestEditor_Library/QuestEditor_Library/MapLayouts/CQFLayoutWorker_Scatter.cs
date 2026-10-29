using Verse;

namespace QuestEditor_Library;

public class CQFLayoutWorker_Scatter : CQFLayoutWorker
{
    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index) => context.ShuffledCells();
}
