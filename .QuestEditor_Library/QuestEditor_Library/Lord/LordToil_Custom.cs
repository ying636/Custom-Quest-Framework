using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace QuestEditor_Library
{
    public class LordToil_Custom : LordToil
    {
        public override void UpdateAllDuties()
        {
            LordJob_Custom lordJob = (LordJob_Custom)this.lord.LordJob;
            this.lord.ownedPawns.ForEach((x) => 
            {
                if (lordJob.pawnDutyDatas.TryGetValue(x, out DutyDef def))
                {
                    PawnDuty duty = new PawnDuty(def);
                    if (def == DutyDefOf.Defend)
                    {
                        if (lordJob.pawnRouteDatas.TryGetValue(x, out RouteData data))
                        {
                            duty.focus = new LocalTargetInfo(data.routue.First());
                        }
                        else if (lordJob.defendDatas != null && lordJob.defendDatas.ContainsKey(x)) 
                        {
                            duty.focus = new LocalTargetInfo(lordJob.defendDatas[x]);
                        }
                        else
                        {
                            duty.focus = new LocalTargetInfo(x.Position);
                        }
                    } 
                    if (x.mindState.duty != null) 
                    {
                        duty.overrideFacing = x.mindState.duty.overrideFacing;
                    }
                    x.mindState.duty = duty;
                }
            });
        }
    }
}
