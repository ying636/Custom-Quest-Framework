using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;

namespace QuestEditor_Library
{
    public class WorkGiver_MoveIn : WorkGiver_Scanner
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn.Map == null || !pawn.Map.designationManager.AnySpawnedDesignationOfDef(QEDefOf.QE_MoveIn);
        }
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> result = new List<Thing>();
            pawn.Map.designationManager.SpawnedDesignationsOfDef(QEDefOf.QE_MoveIn).ToList().ForEach(d => result.Add(d.target.Thing));
            return result;
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return t.Map.designationManager.DesignationOn(t, QEDefOf.QE_MoveIn) != null && t != pawn && pawn.CanReserveAndReach(t,PathEndMode.Touch,Danger.Deadly, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Job result = JobMaker.MakeJob(QEDefOf.QE_MoveInTargetToSubMap,t);
            result.count = t.stackCount;
            return result;
        }
    }
}
