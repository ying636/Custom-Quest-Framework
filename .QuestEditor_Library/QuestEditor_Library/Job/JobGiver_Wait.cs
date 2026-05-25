using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobGiver_Wait : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            IntVec3? pos = ((LordJob_Custom)(pawn.GetLord()?.LordJob))?.pawnRouteDatas.TryGetValue(pawn)?.routue.First();

            if (pawn.Position == pos || pos == null)
            {
                Job result = JobMaker.MakeJob(JobDefOf.Wait, pos == null ? pawn.DutyLocation() : pos.Value);
                result.overrideFacing = pawn.mindState.duty.overrideFacing;
                return result;
            }
            else 
            {
                Job result = JobMaker.MakeJob(JobDefOf.Goto,pos.Value);
                return result;
            }
        }
    }
}
