using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutWorker_Clusters : CQFLayoutWorker
{
    public override void Prepare(CQFLayoutPlanningContext context)
    {
        centers.Clear();
        foreach (IntVec3 cell in context.ShuffledCells(context.Settings.gridSize))
        {
            if (centers.All(c => c.DistanceTo(cell) >= context.Settings.gridSize * 2)) centers.Add(cell);
            if (centers.Count >= context.Settings.clusterCount) break;
        }
        if (centers.Count < context.Settings.clusterCount) throw new CQFLayoutException("CQF_Layout_ClusterSpace");
    }

    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index)
    {
        IntVec3 center = centers[index % centers.Count];
        return context.ShuffledCells().OrderBy(c => c.DistanceTo(center));
    }

    private readonly List<IntVec3> centers = new List<IntVec3>();
}
