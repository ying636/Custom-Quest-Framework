using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public sealed class LordToil_MetaLord : MultLevelLordToilBase
    {
        public override void GiveDutyForPawns(List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns) this.Job.ApplyDuty(pawn);
        }
    }
}
