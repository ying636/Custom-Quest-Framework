using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobGiver_ExitSubMapAndExitMap : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return 5f;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map.Parent is MapParent_Custom parent && parent.exit is Thing target && pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                return JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, target);
            }
            else if (pawn.Map.exitMapGrid.MapUsesExitGrid && RCellFinder.TryFindRandomExitSpot(pawn, out IntVec3 spot))
            {
                Job result = JobMaker.MakeJob(JobDefOf.Goto, spot);
                result.exitMapOnArrival = true;
                return result;
            }

            return null;
        }
    }
}
