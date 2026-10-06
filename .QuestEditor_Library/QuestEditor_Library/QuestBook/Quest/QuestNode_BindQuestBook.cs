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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_BindQuestBook.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public SlateRef<QuestBookDef> bookDef;
    }
}
