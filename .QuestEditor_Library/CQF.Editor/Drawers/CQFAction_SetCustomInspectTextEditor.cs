using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_SetCustomInspectTextEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetCustomInspectText cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomInspectText".Translate(), ref cqfReceiver.text, x, 240f);
            y += 30f;
        }
    }
}
