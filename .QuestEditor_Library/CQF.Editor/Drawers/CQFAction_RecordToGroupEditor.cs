using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_RecordToGroupEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_RecordToGroup cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "RecordKeyOfData".Translate(), cqfReceiver.recordKey, value => cqfReceiver.recordKey = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
        }
    }
}
