using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_RecordToDatabaseEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_RecordToDatabase cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "RecordKeyOfData".Translate(), cqfReceiver.recordKey, value => cqfReceiver.recordKey = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
            Rect rect = new Rect(x, y, 350f, 25f);
            Widgets.CheckboxLabeled(rect, "RecordToTemporaryBase".Translate(), ref cqfReceiver.recordToTemporaryBase);
            TooltipHandler.TipRegion(rect, "RecordToTemporaryBase_Tip".Translate());
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToQuestBase".Translate(), ref cqfReceiver.recordToQuestBase);
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToGlobalBase".Translate(), ref cqfReceiver.recordToGlobalBase);
            y += 30f;
        }
    }
}
