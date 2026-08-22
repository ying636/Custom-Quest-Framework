using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class MainTabWindow_QuestBook : MainTabWindow
    {
        public MainTabWindow_QuestBook()
        {
            nodeCanvas.OpenStepInfo = step => Find.WindowStack.Add(new Dialog_QuestBookStepInfo(step, selectedInstance));
        }

        public MainTabWindow_QuestBook(QuestBookInstance instance) : this()
        {
            selectedInstance = instance;
        }

        public override Vector2 RequestedTabSize => new Vector2(1120f, 700f);

        public override void PreOpen()
        {
            base.PreOpen();
            instances = (GameComponent_QuestBook.Instance?.Instances ?? new List<QuestBookInstance>())
                .Where(instance => instance?.bookDef != null)
                .ToList();
            if (instances.NullOrEmpty())
            {
                Close();
                return;
            }
            if (selectedInstance == null)
            {
                selectedInstance = instances.FirstOrDefault();
            }
            EnsureSelectedChapter();
            nodeCanvas.ResetView();
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Small;
            if (instances.NullOrEmpty())
            {
                Widgets.Label(rect, "CQF_QuestBook_Empty".Translate());
                return;
            }
            Rect bookSelector = new Rect(rect.x, rect.y, 210f, rect.height);
            Rect canvasRect = new Rect(bookSelector.xMax + 8f, rect.y, rect.width - 218f, rect.height);
            DrawBookSelector(bookSelector);
            if (selectedInstance?.bookDef != null)
            {
                EnsureSelectedChapter();
                nodeCanvas.Draw(canvasRect, selectedInstance.bookDef, selectedInstance, false, selectedChapter);
            }
        }

        private void DrawBookSelector(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 28f), "CQF_QuestBook_Label".Translate().Colorize(ColorLibrary.SkyBlue));
            float y = rect.y + 46f;
            foreach (QuestBookInstance instance in instances)
            {
                Rect row = new Rect(rect.x + 6f, y, rect.width - 12f, 32f);
                if (instance == selectedInstance)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                bool hasChapters = instance.bookDef?.chapters.NullOrEmpty() == false;
                Rect labelRect = row;
                if (instance == selectedInstance && hasChapters)
                {
                    Rect chapterToggleRect = new Rect(row.x + 4f, row.y + 2f, 28f, 28f);
                    if (Widgets.ButtonText(chapterToggleRect, chaptersExpanded ? "v" : ">", false))
                    {
                        chaptersExpanded = !chaptersExpanded;
                    }
                    labelRect = new Rect(row.x + 34f, row.y, row.width - 34f, row.height);
                }
                if (Widgets.ButtonText(labelRect, instance.bookDef?.LabelCap ?? instance.instanceId, false))
                {
                    selectedInstance = instance;
                    selectedChapter = instance.bookDef?.FirstChapter;
                    chaptersExpanded = true;
                    nodeCanvas.ResetView();
                }
                y += 36f;
                if (instance != selectedInstance || !hasChapters || !chaptersExpanded)
                {
                    continue;
                }
                foreach (QuestBookChapter chapter in instance.bookDef.chapters)
                {
                    Rect chapterRow = new Rect(rect.x + 30f, y, rect.width - 36f, 28f);
                    if (chapter == selectedChapter)
                    {
                        Widgets.DrawHighlightSelected(chapterRow);
                    }
                    if (Widgets.ButtonText(chapterRow, FormatChapterLabel(chapter, instance.bookDef), false))
                    {
                        selectedChapter = chapter;
                        nodeCanvas.ResetView();
                    }
                    y += 32f;
                }
            }
        }

        private static string FormatChapterLabel(QuestBookChapter chapter, QuestBookDef book)
        {
            string label = chapter?.Label ?? string.Empty;
            return label.Replace("{0}", (book?.chapters.IndexOf(chapter) + 1).ToString());
        }

        private void EnsureSelectedChapter()
        {
            QuestBookDef book = selectedInstance?.bookDef;
            if (book?.chapters.NullOrEmpty() != false)
            {
                selectedChapter = null;
                return;
            }
            if (selectedChapter != null)
            {
                QuestBookChapter matchingChapter = book.chapters.FirstOrDefault(chapter => chapter.id == selectedChapter.id);
                if (matchingChapter != null)
                {
                    selectedChapter = matchingChapter;
                    return;
                }
            }
            selectedChapter = book.FirstChapter;
        }

        private readonly QuestBookNodeCanvas nodeCanvas = new QuestBookNodeCanvas();
        private List<QuestBookInstance> instances = new List<QuestBookInstance>();
        private QuestBookInstance selectedInstance;
        private QuestBookChapter selectedChapter;
        private bool chaptersExpanded = true;
    }
}
