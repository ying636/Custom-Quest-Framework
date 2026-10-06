using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_ApparelEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Apparel cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Apparel modData = pawnDef.DataFor<PawnModData_Apparel>();
            cqfReceiver.RemoveDuplicateLayers(modData.apparels);
            foreach (ApparelLayerDef layer in cqfReceiver.AvailableLayers())
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                ThingData data = cqfReceiver.ApparelForLayer(modData.apparels, layer);
                Rect layerRect = new Rect(row.x + 8f, row.y + 6f, 120f, 24f);
                Widgets.Label(layerRect, cqfReceiver.LayerLabel(layer).Colorize(ColorLibrary.PaleBlue));
                Rect iconRect = new Rect(layerRect.xMax + 8f, row.y + 4f, 28f, 28f);
                if (data?.def?.uiIcon != null)
                {
                    Widgets.DefIcon(iconRect, data.def, cqfReceiver.StuffFor(data.def, data.stuff));
                }

                float deleteWidth = data?.def == null ? 0f : 76f;
                Rect buttonRect = new Rect(iconRect.xMax + 8f, row.y + 3f, row.xMax - iconRect.xMax - deleteWidth - 16f, 30f);
                if (cqfReceiver.DrawTextButton(buttonRect, cqfReceiver.ThingLabel(data)))
                {
                    cqfReceiver.OpenLayerSelectDialog(modData.apparels, layer);
                }

                if (data?.def != null && cqfReceiver.DrawCommandText(new Rect(row.xMax - 76f, row.y + 3f, 68f, 30f), "CQF_PawnEditor_Delete".Translate()))
                {
                    cqfReceiver.ClearLayer(modData.apparels, layer);
                }

                y += 42f;
            }
        }
    }
}
