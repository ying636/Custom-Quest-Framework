using Verse;

namespace QuestEditor_Library;

public abstract class CQFLayoutWorker
{
    public virtual void Prepare(CQFLayoutPlanningContext context) { }
    public virtual int Count(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int requested) => requested;
    public abstract IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index);
    public virtual bool Accept(CQFLayoutPlanningContext context, CQFLayoutPlacement part) => true;
    public virtual void Finish(CQFLayoutPlanningContext context) { }
}
