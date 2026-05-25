using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class CompPropertiesHackOutcome : CompProperties
    {
        public CompPropertiesHackOutcome()
        {
            this.compClass = typeof(CompHackOutcome);
        }

        public List<CQFAction> outcoomes = new List<CQFAction>();
    }
    public class CompHackOutcome : ThingComp
    {
        public CompPropertiesHackOutcome Props => (CompPropertiesHackOutcome)this.props;
        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == "Hackend") 
            {
                foreach (var item in this.Props.outcoomes)
                {
                    item.Work(new Dictionary<string, TargetInfo>() { ["CustomThing"] = this.parent },GameTools.GetQuestFromThing(this.parent));
                }
            }
        }
    }
}
