using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutWorker_Ruins : CQFLayoutWorker_City
{
    public override void Prepare(CQFLayoutPlanningContext context)
    {
        vacant.Clear();
        foreach (IntVec3 cell in base.Locations(context, new CQFLayoutPartRule(), 0))
            if (context.Random.NextDouble() < context.Settings.vacancy) vacant.Add(cell);
    }

    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index) => base.Locations(context, rule, index).Where(c => !vacant.Contains(c));

    private readonly HashSet<IntVec3> vacant = new HashSet<IntVec3>();
}
