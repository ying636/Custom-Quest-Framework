using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestNode_BindQuestBookEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_BindQuestBook cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawSelectButton(x, ref y, "QuestBookDef".Translate(), DefDatabase<QuestBookDef>.AllDefsListForReading, def => cqfReceiver.bookDef = def, def => def.defName);
        }
    }
}
