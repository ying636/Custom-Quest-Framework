using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_QuestBookStepEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_QuestBookStep cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (cqfReceiver.selectedBookDef == null && cqfReceiver.editorBook != null)
            {
                cqfReceiver.selectedBookDef = cqfReceiver.editorBook;
            }

            float width = Mathf.Max(280f, inRect.width - x - 12f);
            string selectedBookLabel = cqfReceiver.selectedBookDef == null ? "CQF_QuestBook_None".Translate().ToString() : QuestEditor_Library.CQFAction_QuestBookStep.GetBookLabel(cqfReceiver.selectedBookDef);
            Rect bookRect = new Rect(x, y, width, 28f);
            if (Widgets.ButtonText(bookRect, "CQF_QuestBook_ActionBook".Translate(selectedBookLabel), false))
            {
                List<QuestBookDef> availableBooks = cqfReceiver.GetAvailableBooks();
                if (availableBooks.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(availableBooks.Select(book => new FloatMenuOption(QuestEditor_Library.CQFAction_QuestBookStep.GetBookLabel(book), () =>
                    {
                        cqfReceiver.selectedBookDef = book;
                        if (!cqfReceiver.GetAvailableSteps().Any(step => step.id == cqfReceiver.stepId))
                        {
                            cqfReceiver.stepId = null;
                        }
                    })).ToList()));
                }
            }

            y += bookRect.height + 6f;
            List<QuestBookStep> availableSteps = cqfReceiver.GetAvailableSteps();
            string selectedLabel = availableSteps.FirstOrDefault(step => step.id == cqfReceiver.stepId)?.Label;
            if (selectedLabel.NullOrEmpty())
            {
                selectedLabel = cqfReceiver.stepId.NullOrEmpty() ? "CQF_QuestBook_None".Translate().ToString() : cqfReceiver.stepId;
            }

            Rect selectRect = new Rect(x, y, width, 28f);
            if (cqfReceiver.selectedBookDef == null)
            {
                Widgets.Label(selectRect, "CQF_QuestBook_SelectBookFirst".Translate().Colorize(Color.gray));
                TooltipHandler.TipRegion(selectRect, "CQF_QuestBook_SelectBookFirst".Translate());
            }
            else if (availableSteps.Any())
            {
                if (Widgets.ButtonText(selectRect, "CQF_QuestBook_ActionStep".Translate(selectedLabel), false))
                {
                    Find.WindowStack.Add(new FloatMenu(availableSteps.Select(step => new FloatMenuOption(step.Label, () => cqfReceiver.stepId = step.id)).ToList()));
                }
            }
            else
            {
                Widgets.Label(selectRect, "CQF_QuestBook_NoStepsAvailable".Translate().Colorize(Color.gray));
                TooltipHandler.TipRegion(selectRect, "CQF_QuestBook_NoStepsAvailable".Translate());
            }

            y += selectRect.height + 8f;
        }
    }
}
