using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_GameUnique : QuestNode
    {
        protected override void RunInt()
        {
          
        }

        protected override bool TestRunInt(Slate slate)
        {
            QuestScriptDef definition = this.questDef ?? QuestGen.quest?.root;
            return definition == null || !Find.QuestManager.QuestsListForReading.Exists(q => q.root == definition);
        }
        public QuestScriptDef questDef;
    }
}
