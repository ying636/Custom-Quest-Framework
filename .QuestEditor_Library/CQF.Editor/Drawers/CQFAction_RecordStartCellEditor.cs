using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_RecordStartCellEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_RecordStartCell cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref cqfReceiver.recordKey, x, 150f);
            y += 30f;
        }
    }
}
