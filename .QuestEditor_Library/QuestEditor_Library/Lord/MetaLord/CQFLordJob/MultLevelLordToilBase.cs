using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public abstract class MultLevelLordToilBase : LordToil
    {
        public MetaLord MetaLord => this.Job.metaLord;
        public MultLevelLordJobBase Job => this.lord.LordJob as MultLevelLordJobBase;

        public override bool CanAddPawn(Pawn pawn)
        {
            if (this.Job?.metaLord == null || this.MetaLord.Disposed) return false;
            float score = this.Job.GetPawnAcceptScore(pawn);
            return score >= 0f && !float.IsNaN(score) && !float.IsInfinity(score);
        }

        public override void UpdateAllDuties()
        {
            List<Pawn> normalPawns = new List<Pawn>();
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                if (this.MetaLord?.GetMove(pawn) != null)
                    pawn.mindState.duty = new PawnDuty(QEDefOf.QE_Duty_MoveLevel);
                else normalPawns.Add(pawn);
            }
            this.GiveDutyForPawns(normalPawns);
        }

        public abstract void GiveDutyForPawns(List<Pawn> pawns);
    }
}
