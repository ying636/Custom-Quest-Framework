using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class JobDriver_Patrol : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (this.pawn.GetLord().LordJob is LordJob_Custom lordJob && lordJob.pawnRouteDatas.TryGetValue(this.pawn, out RouteData data))
            {
                if (this.pawn.CanReach(data.cur, PathEndMode.OnCell, Danger.Deadly))
                {
                    Toil go = Toils_Goto.GotoCell(data.cur, PathEndMode.OnCell);
                    go.AddFinishAction(() =>
                    {
                        if (data.routue.Last() == data.cur)
                        {
                            data.cur = data.routue.First();
                        }
                        data.cur = data.routue[data.routue.IndexOf(data.cur) + 1];
                    });
                    yield return go;
                    yield break;
                }
                else 
                {
                    if (data.routue.Last() == data.cur)
                    {
                        data.cur = data.routue.First();
                    }
                    data.cur = data.routue[data.routue.IndexOf(data.cur) + 1];
                }
            }
            yield break;
        }
    }
}
