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
    public class JobGiver_MoveToNewLevel : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.GetLord() is Lord lord && lord.LordJob 
                is MultLevelLordJobBase lordJob && lordJob.metaLord
                is MetaLord metaLord && metaLord.moves.Find(m => m.pawn
                == pawn) is MoveRequirment move) 
            {
                if (LevelPather.GetPathLine(pawn.Map,move.targetMap) is List<CQFMapPortal>
                    list && list.Last() is CQFMapPortal portal 
                    && pawn.CanReach(portal,PathEndMode.Touch,Danger.Deadly)) 
                {
                    return JobMaker.MakeJob(JobDefOf.EnterPortal,portal);
                }
            }
            return null;
        }
    }
}
