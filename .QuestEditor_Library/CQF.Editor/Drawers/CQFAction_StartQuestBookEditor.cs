using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_StartQuestBookEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_StartQuestBook cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            string selected = cqfReceiver.bookDef == null ? "CQF_QuestBook_None".Translate().ToString() : (cqfReceiver.bookDef.label.NullOrEmpty() ? cqfReceiver.bookDef.defName : cqfReceiver.bookDef.label);
            Rect selectRect = new Rect(x, y, Mathf.Max(280f, inRect.width - x - 12f), 28f);
            if (CQFUIStyle.ButtonText(selectRect, "CQF_QuestBook_ActionStartQuestBook".Translate(selected), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestBookDef>.AllDefsListForReading, definition => cqfReceiver.bookDef = definition, definition => definition.label.NullOrEmpty() ? definition.defName : definition.label);
            }

            y += selectRect.height + 8f;
        }
    }
}
