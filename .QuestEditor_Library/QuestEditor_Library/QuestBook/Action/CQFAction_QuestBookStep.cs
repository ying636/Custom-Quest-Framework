using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class CQFAction_QuestBookStep : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_QuestBookStep.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref stepId, "stepId");
            Scribe_Defs.Look(ref selectedBookDef, "bookDef");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("bookDef", selectedBookDef?.defName));
            result.Add(new XElement("stepId", stepId));
            return result;
        }

        public void SetEditorBook(QuestBookDef book)
        {
            editorBook = book;
        }

        protected QuestBookInstance FindTargetInstance(Quest quest)
        {
            if (selectedBookDef == null)
            {
                return GameComponent_QuestBook.Instance?.FindByQuest(quest);
            }
            QuestBookInstance instance = quest == null
                ? GameComponent_QuestBook.Instance?.Instances.FirstOrDefault(candidate => candidate?.bookDef?.defName == selectedBookDef.defName && candidate.state == QuestBookState.Active)
                : GameComponent_QuestBook.Instance?.FindByQuest(quest);
            return instance?.bookDef?.defName == selectedBookDef.defName ? instance : null;
        }
        internal List<QuestBookStep> GetAvailableSteps()
        {
            QuestBookDef book = selectedBookDef ?? editorBook;
            if (book != null)
            {
                return book.chapters.SelectMany(chapter => chapter.steps).Where(step => step != null).ToList();
            }
            return new List<QuestBookStep>();
        }
        internal List<QuestBookDef> GetAvailableBooks()
        {
            List<QuestBookDef> books = DefDatabase<QuestBookDef>.AllDefsListForReading.ToList();
            if (editorBook != null && !books.Any(book => book.defName == editorBook.defName))
            {
                books.Insert(0, editorBook);
            }
            return books;
        }
        internal static string GetBookLabel(QuestBookDef book)
        {
            return book.label.NullOrEmpty() ? book.defName : book.label;
        }

        [Unsaved(false)]
        internal QuestBookDef editorBook;

        public QuestBookDef selectedBookDef;

        [NoTranslate]
        protected internal string stepId;
    }
}
