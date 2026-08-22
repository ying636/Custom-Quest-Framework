using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookChapterSidebar
    {
        public QuestBookChapter SelectedChapter { get; private set; }

        public QuestBookStep SelectedStep { get; private set; }

        public QuestBookStep HoveredStep { get; private set; }

        public System.Action<QuestBookChapter> OpenChapterEditor { get; set; }

        public float Width => maxWidth;

        public float DisplayedWidth => currentWidth;

        public void Draw(Rect rect, QuestBookDef book, bool editable, float rightLimit = float.PositiveInfinity)
        {
            expandedWidth = Mathf.Min(maxWidth, Mathf.Max(minWidth, rect.width));
            targetWidth = expandedByClick ? expandedWidth : minWidth;
            float previousWidth = currentWidth;
            currentWidth = Mathf.MoveTowards(currentWidth, targetWidth, animationSpeed * Time.unscaledDeltaTime);
            if (!Mathf.Approximately(previousWidth, currentWidth))
            {
                GUI.changed = true;
            }
            if (SelectedChapter == null || !book.chapters.Contains(SelectedChapter))
            {
                SelectedChapter = book.FirstChapter;
            }
            if (SelectedChapter != null && !expandedChapters.Contains(SelectedChapter))
            {
                expandedChapters.Add(SelectedChapter);
            }
            HoveredStep = null;
            Rect panelRect = new Rect(rect.x, rect.y, currentWidth, rect.height);
            Widgets.DrawMenuSection(panelRect);
            if (currentWidth < minWidth + 8f)
            {
                HoveredStep = null;
                if (Widgets.ButtonText(new Rect(panelRect.x + 7f, panelRect.y + 8f, 30f, 30f), ">", false))
                {
                    expandedByClick = true;
                }
                TooltipHandler.TipRegion(panelRect, "CQF_QuestBook_Chapters".Translate());
                DrawHoverDetails(rect, rightLimit);
                return;
            }
            Widgets.Label(new Rect(panelRect.x + 12f, panelRect.y + 10f, currentWidth - 100f, 28f), "CQF_QuestBook_Chapters".Translate().Colorize(ColorLibrary.SkyBlue));
            if (Widgets.ButtonText(new Rect(panelRect.xMax - 38f, panelRect.y + 8f, 30f, 30f), "<", false))
            {
                expandedByClick = false;
            }
            if (Widgets.ButtonText(new Rect(panelRect.xMax - 76f, panelRect.y + 8f, 30f, 30f), "+", false))
            {
                QuestBookChapter chapter = new QuestBookChapter
                {
                    id = "chapter_" + (book.chapters.Count + 1),
                    labelKey = "CQF_QuestBook_Chapter".Translate(book.chapters.Count + 1).ToString()
                };
                book.chapters.Add(chapter);
                SelectedChapter = chapter;
                expandedChapters.Add(chapter);
            }
            float y = panelRect.y + 48f;
            foreach (QuestBookChapter chapter in book.chapters.ToList())
            {
                Rect chapterRect = new Rect(panelRect.x + 8f, y, currentWidth - 16f, 34f);
                if (chapter == SelectedChapter)
                {
                    Widgets.DrawHighlightSelected(chapterRect);
                }
                bool expanded = expandedChapters.Contains(chapter);
                if (Widgets.ButtonText(new Rect(chapterRect.x, chapterRect.y, 28f, chapterRect.height), expanded ? "v" : ">", false))
                {
                    if (expanded)
                    {
                        expandedChapters.Remove(chapter);
                    }
                    else
                    {
                        expandedChapters.Add(chapter);
                    }
                }
                string chapterLabel = chapter.Label.Replace("{0}", (book.chapters.IndexOf(chapter) + 1).ToString());
                if (Widgets.ButtonText(new Rect(chapterRect.x + 30f, chapterRect.y, chapterRect.width - 66f, chapterRect.height), chapterLabel, false))
                {
                    SelectedChapter = chapter;
                    SelectedStep = null;
                }
                if (chapterRect.Contains(UnityEngine.Event.current.mousePosition) && UnityEngine.Event.current.type == EventType.MouseDown && UnityEngine.Event.current.button == 1)
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CQF_QuestBook_EditChapter".Translate(), () => OpenChapterEditor?.Invoke(chapter)),
                        new FloatMenuOption("CQF_QuestBook_DeleteChapter".Translate(), () => DeleteChapter(book, chapter))
                    }));
                    UnityEngine.Event.current.Use();
                }
                if (Widgets.ButtonText(new Rect(chapterRect.xMax - 30f, chapterRect.y, 30f, chapterRect.height), "+", false))
                {
                    chapter.steps.Add(new QuestBookStep
                    {
                        id = chapter.id + "_step_" + (chapter.steps.Count + 1),
                        labelKey = "CQF_QuestBook_Step".Translate().ToString()
                    });
                    SelectedChapter = chapter;
                    expandedChapters.Add(chapter);
                }
                y += 38f;
                if (!expanded)
                {
                    continue;
                }
                foreach (QuestBookStep step in chapter.steps.ToList())
                {
                    Rect stepRect = new Rect(panelRect.x + 28f, y, currentWidth - 36f, 30f);
                    if (step == SelectedStep)
                    {
                        Widgets.DrawHighlightSelected(stepRect);
                    }
                    if (stepRect.Contains(UnityEngine.Event.current.mousePosition))
                    {
                        HoveredStep = step;
                    }
                    if (Widgets.ButtonText(stepRect, step.Label, false))
                    {
                        SelectedChapter = chapter;
                        SelectedStep = step;
                    }
                    y += 32f;
                }
            }
            DrawHoverDetails(rect, rightLimit);
        }

        public void SelectStep(QuestBookStep step, QuestBookChapter chapter)
        {
            SelectedChapter = chapter;
            SelectedStep = step;
            expandedChapters.Add(chapter);
        }

        private void DeleteChapter(QuestBookDef book, QuestBookChapter chapter)
        {
            book.chapters.Remove(chapter);
            expandedChapters.Remove(chapter);
            if (SelectedChapter == chapter)
            {
                SelectedChapter = book.FirstChapter;
                SelectedStep = null;
            }
        }

        private void DrawHoverDetails(Rect rect, float rightLimit)
        {
            if (rect.width < 200f)
            {
                detailAlpha = 0f;
                return;
            }
            detailTargetAlpha = HoveredStep == null ? 0f : 1f;
            if (HoveredStep != null)
            {
                detailStep = HoveredStep;
            }
            detailAlpha = Mathf.MoveTowards(detailAlpha, detailTargetAlpha, 4f * Time.unscaledDeltaTime);
            detailOffset = Mathf.MoveTowards(detailOffset, detailTargetAlpha > 0f ? 0f : 8f, 32f * Time.unscaledDeltaTime);
            if (detailAlpha <= 0.01f || detailStep == null)
            {
                return;
            }
            float panelWidth = 270f;
            float panelHeight = 96f;
            float panelX = Mathf.Min(rect.x + rect.width + 8f, rightLimit - panelWidth);
            if (panelX < rect.x + 4f)
            {
                panelX = rect.x + 4f;
            }
            float panelY = Mathf.Clamp(UnityEngine.Event.current.mousePosition.y + 12f + detailOffset, rect.y + 4f, rect.yMax - panelHeight - 4f);
            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, detailAlpha);
            Widgets.DrawBoxSolid(panelRect, new Color(0.035f, 0.045f, 0.055f, 0.98f * detailAlpha));
            Widgets.DrawBox(panelRect, 1);
            Widgets.Label(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 24f), detailStep.Label.Colorize(ColorLibrary.SkyBlue));
            if (!detailStep.Description.NullOrEmpty())
            {
                Widgets.Label(new Rect(panelRect.x + 10f, panelRect.y + 34f, panelRect.width - 20f, 52f), detailStep.Description);
            }
            GUI.color = oldColor;
            GUI.changed = true;
        }

        private readonly HashSet<QuestBookChapter> expandedChapters = new HashSet<QuestBookChapter>();
        private const float minWidth = 44f;
        private const float maxWidth = 252f;
        private const float animationSpeed = 900f;
        private float currentWidth = minWidth;
        private float targetWidth = minWidth;
        private float expandedWidth = maxWidth;
        private bool expandedByClick;
        private float detailAlpha;
        private float detailTargetAlpha;
        private float detailOffset = 8f;
        private QuestBookStep detailStep;
    }
}
