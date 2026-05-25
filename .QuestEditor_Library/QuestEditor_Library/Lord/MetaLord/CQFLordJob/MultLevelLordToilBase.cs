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
    public abstract class MultLevelLordToilBase : LordToil
    {
        public MetaLord MetaLord => this.Job.metaLord;
        public MultLevelLordJobBase Job => this.lord.LordJob as MultLevelLordJobBase;
        public override void UpdateAllDuties()
        {
            MetaLord lord = this.MetaLord;
            var pawns = this.lord.ownedPawns.ListFullCopy();
            foreach (var pawn in this.lord.ownedPawns)
            {
                if (lord.moves.Exists(m => m.pawn == pawn)) 
                {
                    pawn.mindState.duty = new PawnDuty(QEDefOf.QE_Duty_MoveLevel);
                    pawns.Remove(pawn);
                }
            }
            this.GiveDutyForPawns(pawns);
        }


        public abstract void GiveDutyForPawns(List<Pawn> pawns);
    }
}
