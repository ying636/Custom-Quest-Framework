using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_DeleteEventAreaEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_DeleteEventArea cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "EventAreaKey".Translate(), ref cqfReceiver.key, x, 150f);
            y += 30f;
        }
    }
}
