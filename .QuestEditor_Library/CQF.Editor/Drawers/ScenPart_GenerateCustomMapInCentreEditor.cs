using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace QuestEditor_Library
{
    public static class ScenPart_GenerateCustomMapInCentreEditor
    {
        public static void DoEditInterface_0(QuestEditor_Library.ScenPart_GenerateCustomMapInCentre cqfReceiver, Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(cqfReceiver, cqfReceiver.maps.Count * 30f + ScenPart.RowHeight);
            CQFEditorTools.DrawButtonWithIcon(scenPartRect.y, () => CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, d => cqfReceiver.maps.Add(d), d => d.label), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.maps, d => cqfReceiver.maps.Remove(d), d => d.label), scenPartRect.width + 20f, 20);
            scenPartRect.y += 30f;
            cqfReceiver.maps.ForEach(m =>
            {
                Widgets.Label(scenPartRect, m.label);
                scenPartRect.y += 30f;
            });
        }
    }
}
