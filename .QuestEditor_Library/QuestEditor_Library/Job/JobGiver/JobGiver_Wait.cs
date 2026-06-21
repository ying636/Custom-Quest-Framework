using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobGiver_Wait : JobGiver_TargetBase
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            IntVec3? pos = this.GetWaitPosition(pawn);

            if (pawn.Position == pos || pos == null)
            {
                Job result = JobMaker.MakeJob(JobDefOf.Wait_Combat, pos == null ? pawn.DutyLocation() : pos.Value);
                result.overrideFacing = pawn.mindState.duty.overrideFacing;
                result.expiryInterval = 60;
                result.checkOverrideOnExpire = true;
                return result;
            }
            else 
            {
                Job result = JobMaker.MakeJob(JobDefOf.Goto,pos.Value);
                result.expiryInterval = 120;
                result.checkOverrideOnExpire = true;
                return result;
            }
        }

        private IntVec3? GetWaitPosition(Pawn pawn)
        {
            if (this.TryGetTarget(pawn, out TargetInfo target))
            {
                return target.HasThing ? target.Thing.Position : target.Cell;
            }
            if (pawn.GetLord()?.LordJob is LordJob_Custom lordJob
                && lordJob.pawnRouteDatas.TryGetValue(pawn, out RouteData routeData)
                && !routeData.routue.NullOrEmpty())
            {
                return routeData.routue.First();
            }
            return null;
        }
    }
}
