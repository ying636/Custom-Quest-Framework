using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class CustomDutyTrigger_LordPawnCountBelowEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomDutyTrigger_LordPawnCountBelow cqfReceiver, ref float y, Rect inRect, float x)
        {
            CustomDutyTriggerEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_PawnCount".Translate(), ref cqfReceiver.count, ref cqfReceiver.buffer, x, 150f);
            y += 30f;
        }
    }
}
