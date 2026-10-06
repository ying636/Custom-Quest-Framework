using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestBookObjective_ResearchEditor
    {
        public static void DrawSpecial_0(QuestEditor_Library.QuestBookObjective_Research cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                cqfReceiver.DrawRowLabel(card, rowY, "CQF_QuestBook_TargetResearch");
                string label = cqfReceiver.TargetResearch == null ? "CQF_QuestBook_None".Translate().ToString() : cqfReceiver.TargetResearch.LabelCap;
                Rect button = new Rect(card.x + 184f, rowY, card.width - 198f, 28f);
                if (Widgets.ButtonText(button, label, false, true))
                {
                    List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
                    Find.WindowStack.Add(new Dialog_Select<ResearchProjectDef>(new TextSelectDrawer<ResearchProjectDef>(projects, def => def.LabelCap, def => cqfReceiver.TargetResearch = def, null, def => def.description, null, def => def.defName, null, null), "CQF_QuestBook_TargetResearch".Translate()));
                }

                rowY += 36f;
            });
        }
    }
}
