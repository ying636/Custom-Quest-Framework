using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobGiver_MoveToNewLevel : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            MetaLord owner = (pawn.GetLord()?.LordJob as MultLevelLordJobBase)?.metaLord;
            if (owner != null && owner.TryGetNextPortal(pawn, out CQFMapPortal portal))
                return JobMaker.MakeJob(QEDefOf.CQF_MetaLordMove, portal);
            return null;
        }
    }
}
