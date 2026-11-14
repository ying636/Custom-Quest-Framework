using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutWorker_Village : CQFLayoutWorker
{
    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index)
    {
        IntVec3 center = context.Reference?.Bounds.CenterCell ?? context.Bounds.CenterCell;
        double desiredAngle = index * 2.399963229728653;
        List<IntVec3> candidates = context.ShuffledCells();
        return candidates.OrderBy(c =>
        {
            double angle = Math.Atan2(c.z - center.z, c.x - center.x);
            double delta = Math.Abs(Math.Atan2(Math.Sin(angle - desiredAngle), Math.Cos(angle - desiredAngle)));
            double radius = Math.Sqrt((double)(c.x - center.x) * (c.x - center.x) + (double)(c.z - center.z) * (c.z - center.z));
            return delta * context.Settings.gridSize + (context.Reference == null ? radius * 0.25 : 0);
        });
    }
}
