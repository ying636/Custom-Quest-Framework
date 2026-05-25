using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobGiver_Patrol : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.IsFighting() && !pawn.Downed) 
            {
                if (pawn.GetLord().LordJob is LordJob_Custom lordJob && lordJob.pawnRouteDatas.ContainsKey(pawn))
                {
                    return JobMaker.MakeJob(QEDefOf.QE_Patrol); 
                }
            }
            return null;
        }
    }
}
