using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class CustomDutyTrigger_SignalEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomDutyTrigger_Signal cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CustomDutyTriggerEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFSignalEditor.DrawBookField(y, "InSignal".Translate(), cqfReceiver.signal, value => cqfReceiver.signal = value, x, 150f, inRect.width - x - 12f);
            y += 30f;
            Rect rect = new Rect(x, y, 260f, 25f);
            Widgets.CheckboxLabeled(rect, "CQF_DutySignalAddQuestPrefix".Translate(), ref cqfReceiver.addQuestPrefix);
            if ("CQF_DutySignalAddQuestPrefix_Tip".CanTranslate())
            {
                TooltipHandler.TipRegion(rect, "CQF_DutySignalAddQuestPrefix_Tip".Translate());
            }

            y += 30f;
        }
    }
}
