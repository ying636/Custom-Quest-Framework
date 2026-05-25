using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public abstract class MultLevelLordJobBase : LordJob
    {
        public virtual float GetPawnAcceptScore(Pawn pawn) 
        {
            return 100f;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.metaLord, "metaLord");
        }

        public MetaLord metaLord;
    }
}
