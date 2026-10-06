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
    public static class ScenPart_GenerateCustomMapEditor
    {
        public static void DoEditInterface_0(QuestEditor_Library.ScenPart_GenerateCustomMap cqfReceiver, Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(cqfReceiver, 30f + ScenPart.RowHeight);
            if (Widgets.ButtonText(scenPartRect, "StartMap".Translate(cqfReceiver.map?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, d => cqfReceiver.map = d, d => d.label);
            }
        }
    }
}
