using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_AddQuestDescriptionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_AddQuestDescription cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFQuestDescription".Translate(), ref cqfReceiver.description, x, 240f);
            y += 30f;
        }
    }
}
