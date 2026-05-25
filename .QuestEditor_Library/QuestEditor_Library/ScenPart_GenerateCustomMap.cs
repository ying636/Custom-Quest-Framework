using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ScenPart_GenerateCustomMap : ScenPart
    {
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this,30f + ScenPart.RowHeight);
            if (Widgets.ButtonText(scenPartRect,"StartMap".Translate(this.map?.label),false)) 
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading,d => this.map = d,d =>d.label);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.map, "map");
        }


        public CustomMapDataDef map;
    }
}
