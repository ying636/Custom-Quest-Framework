using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_DoActionForGroupEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_DoActionForGroup cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref cqfReceiver.recordKey, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
    }
}
