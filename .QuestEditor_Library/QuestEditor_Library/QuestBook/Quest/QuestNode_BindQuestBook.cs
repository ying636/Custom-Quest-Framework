using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_BindQuestBook : QuestNode, IDrawable
    {
        protected override void RunInt()
        {
            QuestBookDef def = bookDef.GetValue(QuestGen.slate);
            if (def == null || QuestGen.quest == null)
            {
                Log.Error("CQF task book binding requires a QuestBookDef and an active Quest.");
                return;
            }
            QuestPart_QuestBookBinding part = QuestGen.quest.AddPart<QuestPart_QuestBookBinding>();
            part.bookDef = def;
        }

        protected override bool TestRunInt(Slate slate)
        {
            return bookDef.GetValue(slate) != null;
        }

        public void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawSelectButton(x, ref y, "QuestBookDef".Translate(), DefDatabase<QuestBookDef>.AllDefsListForReading, def => bookDef = def, def => def.defName);
        }

        public SlateRef<QuestBookDef> bookDef;
    }
}
