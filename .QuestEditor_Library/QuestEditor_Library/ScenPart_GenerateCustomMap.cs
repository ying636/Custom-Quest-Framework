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
            if (!CQFEditorBridge.IsLoaded)
            {
                Widgets.Label(listing.GetScenPartRect(this, ScenPart.RowHeight * 3f), "CQF_Editor_NotLoaded".Translate());
                return;
            }

            object[] arguments = new object[]
            {
                listing
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ScenPart_GenerateCustomMap.DoEditInterface(None:Verse.Listing_ScenEdit)", this, arguments);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.map, "map");
        }


        public CustomMapDataDef map;
    }
}
