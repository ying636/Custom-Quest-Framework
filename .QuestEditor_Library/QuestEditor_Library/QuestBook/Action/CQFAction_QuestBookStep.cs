using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class CQFAction_QuestBookStep : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            if (this.selectedBookDef == null && this.editorBook != null)
            {
                this.selectedBookDef = this.editorBook;
            }

            float width = Mathf.Max(280f, inRect.width - x - 12f);
            string selectedBookLabel = this.selectedBookDef == null ? "CQF_QuestBook_None".Translate().ToString() : QuestEditor_Library.CQFAction_QuestBookStep.GetBookLabel(this.selectedBookDef);
            Rect bookRect = new Rect(x, y, width, 28f);
            if (CQFUIStyle.ButtonText(bookRect, "CQF_QuestBook_ActionBook".Translate(selectedBookLabel), false))
            {
                List<QuestBookDef> availableBooks = this.GetAvailableBooks();
                if (availableBooks.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(availableBooks.Select(book => new FloatMenuOption(QuestEditor_Library.CQFAction_QuestBookStep.GetBookLabel(book), () =>
                    {
                        this.selectedBookDef = book;
                        if (!this.GetAvailableSteps().Any(step => step.id == this.stepId))
                        {
                            this.stepId = null;
                        }
                    })).ToList()));
                }
            }

            y += bookRect.height + 6f;
            List<QuestBookStep> availableSteps = this.GetAvailableSteps();
            string selectedLabel = availableSteps.FirstOrDefault(step => step.id == this.stepId)?.Label;
            if (selectedLabel.NullOrEmpty())
            {
                selectedLabel = this.stepId.NullOrEmpty() ? "CQF_QuestBook_None".Translate().ToString() : this.stepId;
            }

            Rect selectRect = new Rect(x, y, width, 28f);
            if (this.selectedBookDef == null)
            {
                Widgets.Label(selectRect, "CQF_QuestBook_SelectBookFirst".Translate().Colorize(CQFUIStyle.Muted));
                TooltipHandler.TipRegion(selectRect, "CQF_QuestBook_SelectBookFirst".Translate());
            }
            else if (availableSteps.Any())
            {
                if (CQFUIStyle.ButtonText(selectRect, "CQF_QuestBook_ActionStep".Translate(selectedLabel), false))
                {
                    Find.WindowStack.Add(new FloatMenu(availableSteps.Select(step => new FloatMenuOption(step.Label, () => this.stepId = step.id)).ToList()));
                }
            }
            else
            {
                Widgets.Label(selectRect, "CQF_QuestBook_NoStepsAvailable".Translate().Colorize(CQFUIStyle.Muted));
                TooltipHandler.TipRegion(selectRect, "CQF_QuestBook_NoStepsAvailable".Translate());
            }

            y += selectRect.height + 8f;
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

            QuestBookInstance instance = quest == null ? GameComponent_QuestBook.Instance?.Instances.FirstOrDefault(candidate => candidate?.bookDef?.defName == selectedBookDef.defName && candidate.state == QuestBookState.Active) : GameComponent_QuestBook.Instance?.FindByQuest(quest);
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
