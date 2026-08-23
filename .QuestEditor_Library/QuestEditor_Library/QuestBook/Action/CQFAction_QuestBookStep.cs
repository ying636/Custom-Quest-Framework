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
            base.Draw(ref y, inRect, x);
            if (selectedBookDef == null && editorBook != null)
            {
                selectedBookDef = editorBook;
            }
            float width = Mathf.Max(280f, inRect.width - x - 12f);
            string selectedBookLabel = selectedBookDef == null ? "CQF_QuestBook_None".Translate().ToString() : GetBookLabel(selectedBookDef);
            Rect bookRect = new Rect(x, y, width, 28f);
            if (Widgets.ButtonText(bookRect, "CQF_QuestBook_ActionBook".Translate(selectedBookLabel), false))
            {
                List<QuestBookDef> availableBooks = GetAvailableBooks();
                if (availableBooks.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(availableBooks.Select(book =>
                        new FloatMenuOption(GetBookLabel(book), () =>
                        {
                            selectedBookDef = book;
                            if (!GetAvailableSteps().Any(step => step.id == stepId))
                            {
                                stepId = null;
                            }
                        })).ToList()));
                }
            }
            y += bookRect.height + 6f;
            List<QuestBookStep> availableSteps = GetAvailableSteps();
            string selectedLabel = availableSteps.FirstOrDefault(step => step.id == stepId)?.Label;
            if (selectedLabel.NullOrEmpty())
            {
                selectedLabel = stepId.NullOrEmpty() ? "CQF_QuestBook_None".Translate().ToString() : stepId;
            }
            Rect selectRect = new Rect(x, y, width, 28f);
            if (selectedBookDef == null)
            {
                Widgets.Label(selectRect, "CQF_QuestBook_SelectBookFirst".Translate().Colorize(Color.gray));
                TooltipHandler.TipRegion(selectRect, "CQF_QuestBook_SelectBookFirst".Translate());
            }
            else if (availableSteps.Any())
            {
                if (Widgets.ButtonText(selectRect, "CQF_QuestBook_ActionStep".Translate(selectedLabel), false))
                {
                    Find.WindowStack.Add(new FloatMenu(availableSteps.Select(step =>
                        new FloatMenuOption(step.Label, () => stepId = step.id)).ToList()));
                }
            }
            else
            {
                Widgets.Label(selectRect, "CQF_QuestBook_NoStepsAvailable".Translate().Colorize(Color.gray));
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
            QuestBookInstance instance = quest == null
                ? GameComponent_QuestBook.Instance?.Instances.FirstOrDefault(candidate => candidate?.bookDef?.defName == selectedBookDef.defName && candidate.state == QuestBookState.Active)
                : GameComponent_QuestBook.Instance?.FindByQuest(quest);
            return instance?.bookDef?.defName == selectedBookDef.defName ? instance : null;
        }

        private List<QuestBookStep> GetAvailableSteps()
        {
            QuestBookDef book = selectedBookDef ?? editorBook;
            if (book != null)
            {
                return book.chapters.SelectMany(chapter => chapter.steps).Where(step => step != null).ToList();
            }
            return new List<QuestBookStep>();
        }

        private List<QuestBookDef> GetAvailableBooks()
        {
            List<QuestBookDef> books = DefDatabase<QuestBookDef>.AllDefsListForReading.ToList();
            if (editorBook != null && !books.Any(book => book.defName == editorBook.defName))
            {
                books.Insert(0, editorBook);
            }
            return books;
        }

        private static string GetBookLabel(QuestBookDef book)
        {
            return book.label.NullOrEmpty() ? book.defName : book.label;
        }

        [Unsaved(false)]
        private QuestBookDef editorBook;

        public QuestBookDef selectedBookDef;

        [NoTranslate]
        protected string stepId;
    }
}
