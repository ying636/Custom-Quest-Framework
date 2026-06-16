using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class LordToil_ComplexCustom : LordToil
    {
        public override void UpdateAllDuties()
        {
            if (!(this.lord.LordJob is LordJob_ComplexCustom lordJob))
            {
                return;
            }
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                lordJob.ApplyDuty(pawn);
            }
        }
    }
}
