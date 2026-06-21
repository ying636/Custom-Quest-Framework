using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class CompPropertiesTriggerDialog : CompProperties 
    {
        public CompPropertiesTriggerDialog() { this.compClass = typeof(CompTriggerDialog); }

        public string triggerSignal = null;
        public DialogTreeDef dialog;
    }
    public class CompTriggerDialog : ThingComp
    {
        public CompPropertiesTriggerDialog Props => (CompPropertiesTriggerDialog)this.props;
        public override void ReceiveCompSignal(string signal)
        {
            if (this.Props.triggerSignal != null && signal == this.Props.triggerSignal) 
            {
                this.Props.dialog?.CreateCQFDialog(this.parent,null,GameTools.GetQuestFromThing(this.parent));
            }
        }
    }
}
