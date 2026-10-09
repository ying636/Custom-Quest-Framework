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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            string source_text = cqfReceiver.text.CanTranslate() ? cqfReceiver.text.Translate().ToString() : cqfReceiver.text;
            string edited_text = source_text;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomInspectText".Translate(), ref edited_text, x, 240f);
            if (edited_text != source_text) cqfReceiver.text = edited_text;
            y += 30f;
        }
    }
}
