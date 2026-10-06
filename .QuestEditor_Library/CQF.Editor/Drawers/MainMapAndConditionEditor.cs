using RimWorld;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class MainMapAndConditionEditor
    {
        public static void Draw_0(QuestEditor_Library.MainMapAndCondition cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "MainMapConditionName".Translate(), ref cqfReceiver.name, x, 200f);
            TooltipHandler.TipRegion(new Rect(x, y, 405f, 25f), "MainMapConditionNameTip".Translate());
            y += 30f;
            if (cqfReceiver.set == null)
            {
                cqfReceiver.set = new CustomMapGenerationSet();
            }

            Rect generationSetRect = new Rect(x, y, 255f, 25f);
            Widgets.Label(generationSetRect, "MainMapGenerationSet".Translate().Colorize(ColorLibrary.PaleBlue));
            TooltipHandler.TipRegion(generationSetRect, "MainMapGenerationSetTip".Translate());
            y += 30f;
            cqfReceiver.set.Draw(ref y, inRect, x + 15f);
            float conditionsY = y;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, cqfReceiver.conditions, inRect, "MainMapConditions".Translate(), condition => condition.GetType().Name.Translate());
            TooltipHandler.TipRegion(new Rect(x, conditionsY, 255f, 25f), "MainMapConditionsTip".Translate());
        }
    }
}
