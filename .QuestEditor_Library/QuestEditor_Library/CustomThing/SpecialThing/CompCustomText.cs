using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class CompCustomText : ThingComp
    {
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.useCustomName, "useCustomName");
            Scribe_Values.Look(ref this.useCustomDescription, "useCustomDescription");
            Scribe_Values.Look(ref this.customName, "customName");
            Scribe_Values.Look(ref this.customDescription, "customDescription");
        }

        public bool useCustomName = false;
        public bool useCustomDescription = false;
        public string customName;
        public string customDescription;
    }
}
