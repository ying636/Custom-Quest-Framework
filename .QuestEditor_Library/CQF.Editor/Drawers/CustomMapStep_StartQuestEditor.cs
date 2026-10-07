using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapStep_StartQuestEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapStep_StartQuest cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            float width = inRect.width - x - 12f;
            Widgets.Label(new Rect(x, y, width, 30f), "CustomMapStep_StartQuest".Translate().Colorize(CQFUIStyle.Accent));
            y += 35f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), cqfReceiver.quest?.label ?? cqfReceiver.quest?.defName ?? "CQF_NotSelected".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading, q => cqfReceiver.quest = q, q => q.label ?? q.defName);
            }

            y += 35f;
            Rect letterRect = new Rect(x, y, width, 30f);
            Widgets.DrawHighlightIfMouseover(letterRect);
            Widgets.CheckboxLabeled(letterRect.ContractedBy(6f, 2f), "CQF_StartQuest_SendLetter".Translate(), ref cqfReceiver.sendAvailableLetter);
            y += 35f;
        }
    }
}
