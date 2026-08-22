using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_QuestBook : Page
    {
        public QuestEditor_QuestBook()
        {
            doCloseX = true;
            absorbInputAroundWindow = false;
            curDef = new QuestBookDef();
            nodeCanvas.OpenStepEditor = step => Find.WindowStack.Add(new Dialog_EditQuestBookStep(step));
            chapterSidebar.OpenChapterEditor = chapter => Find.WindowStack.Add(new Dialog_EditQuestBookChapter(chapter));
        }

        public override string PageTitle => "CQF_QuestBookEditor".Translate().Colorize(ColorLibrary.SkyBlue);

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            DrawToolbar(inRect);
            DrawBookHeader(inRect);
            float sidebarWidth = Mathf.Min(chapterSidebar.Width, Mathf.Max(44f, inRect.width * 0.34f));
            Rect sidebarRect = new Rect(5f, 128f, sidebarWidth, inRect.height - 138f);
            chapterSidebar.Draw(sidebarRect, curDef, true, inRect.width - 10f);
            QuestBookChapter chapter = chapterSidebar.SelectedChapter;
            if (chapter == null)
            {
                Widgets.Label(new Rect(sidebarRect.xMax + 12f, sidebarRect.y + 14f, inRect.width - sidebarRect.xMax - 20f, 30f), "CQF_QuestBook_NoChapter".Translate());
                return;
            }
            float contentX = 5f + chapterSidebar.DisplayedWidth + 12f;
            Rect chapterHeader = new Rect(contentX, sidebarRect.y, inRect.width - contentX - 8f, 38f);
            Widgets.DrawMenuSection(chapterHeader);
            int chapterNumber = curDef.chapters.IndexOf(chapter) + 1;
            string chapterLabel = chapter.Label.Replace("{0}", chapterNumber.ToString());
            Widgets.Label(new Rect(chapterHeader.x + 12f, chapterHeader.y + 7f, chapterHeader.width - 120f, 24f), chapterLabel.Colorize(ColorLibrary.SkyBlue));
            Widgets.Label(new Rect(chapterHeader.x + chapterHeader.width - 108f, chapterHeader.y + 8f, 72f, 22f), "CQF_QuestBook_StepCount".Translate(chapter.steps.Count).Colorize(Color.gray));
            if (Widgets.ButtonText(new Rect(chapterHeader.xMax - 34f, chapterHeader.y + 5f, 28f, 28f), "...", false))
            {
                Find.WindowStack.Add(new Dialog_EditQuestBookChapter(chapter));
            }
            Rect canvasRect = new Rect(chapterHeader.x, chapterHeader.yMax + 8f, chapterHeader.width, inRect.height - 184f);
            nodeCanvas.Draw(canvasRect, curDef, null, true, chapter);
            if (chapterSidebar.SelectedStep != null)
            {
                nodeCanvas.SelectStep(chapterSidebar.SelectedStep);
            }
        }

        private void DrawToolbar(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(5f, 42f, 100f, 30f), "CQF_QuestBook_Load".Translate()))
            {
                List<FloatMenuOption> options = DefDatabase<QuestBookDef>.AllDefsListForReading
                    .Select(def => new FloatMenuOption(def.defName, () =>
                    {
                        curDef = def;
                        PrepareEditableText(curDef);
                        nodeCanvas.ResetView();
                    })).ToList();
                if (options.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                else
                {
                    Messages.Message("CQF_QuestBook_NoLoadedBooks".Translate(), MessageTypeDefOf.CautionInput);
                }
            }
            if (Widgets.ButtonText(new Rect(110f, 42f, 100f, 30f), "CQF_QuestBook_Save".Translate()))
            {
                SaveCurrent();
            }
            if (Widgets.ButtonText(new Rect(215f, 42f, 100f, 30f), "CQF_QuestBook_New".Translate()))
            {
                curDef = new QuestBookDef();
                nodeCanvas.ResetView();
            }
            Widgets.CheckboxLabeled(new Rect(330f, 42f, 180f, 30f), "CQF_QuestBook_AutoStart".Translate(), ref curDef.autoStart, placeCheckboxNearText: false);
        }

        private void DrawBookHeader(Rect inRect)
        {
            const float rowY = 86f;
            float labelWidth = 136f;
            float gap = 28f;
            float availableWidth = Mathf.Max(360f, inRect.width - 10f);
            float fieldWidth = Mathf.Max(150f, (availableWidth - labelWidth * 2f - gap) * 0.5f);
            float firstX = 5f;
            float secondX = firstX + labelWidth + fieldWidth + gap;
            Widgets.Label(new Rect(firstX, rowY, labelWidth, 25f), "CQF_QuestBook_DefName".Translate());
            curDef.defName = Widgets.TextField(new Rect(firstX + labelWidth, rowY, fieldWidth, 25f), curDef.defName);
            Widgets.Label(new Rect(secondX, rowY, labelWidth, 25f), "CQF_QuestBook_LabelField".Translate());
            curDef.label = Widgets.TextField(new Rect(secondX + labelWidth, rowY, fieldWidth, 25f), curDef.label);
        }

        private void AddChapter()
        {
            QuestBookChapter chapter = new QuestBookChapter
            {
                id = "chapter_" + (curDef.chapters.Count + 1),
                labelKey = "CQF_QuestBook_Chapter".Translate(curDef.chapters.Count + 1).ToString()
            };
            chapter.steps.Add(new QuestBookStep
            {
                id = chapter.id + "_step_1",
                labelKey = "CQF_QuestBook_Step".Translate().ToString()
            });
            curDef.chapters.Add(chapter);
        }

        private void SaveCurrent()
        {
            if (curDef.defName.NullOrEmpty())
            {
                Messages.Message("CQF_QuestBook_NoName".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            try
            {
                string directory = Path.Combine(Page_QuestEditor.Path, "QuestBook");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, curDef.defName + ".xml");
                XElement language;
                XElement compiledBook = BuildCompiledBookXml(out language);
                new XElement("Defs", compiledBook).Save(path);
                language.Save(Path.Combine(directory, curDef.defName + "_Text.xml"));
                CQFQuestDefBootstrap.HotLoadQuestBookDef(curDef);
                Messages.Message("CQF_QuestBook_SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception exception)
            {
                Log.Error("CQF task book save error: " + exception);
                Messages.Message("CQF_QuestBook_SaveFailed".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        private XElement BuildCompiledBookXml(out XElement language)
        {
            XElement result = new XElement(curDef.SaveToXElement("QuestEditor_Library.QuestBookDef"));
            language = new XElement("LanguageData");
            string prefix = MakeTextKeyPrefix();
            XElement chapters = result.Element("chapters");
            if (chapters != null)
            {
                foreach (XElement chapter in chapters.Elements("li"))
                {
                    string chapterId = chapter.Element("id")?.Value ?? "chapter";
                    string chapterPrefix = prefix + "_Chapter_" + MakeSafeKeyPart(chapterId);
                    CompileTextElement(chapter.Element("labelKey"), chapterPrefix + "_Label", language);
                    CompileTextElement(chapter.Element("descriptionKey"), chapterPrefix + "_Description", language);
                    XElement steps = chapter.Element("steps");
                    if (steps == null)
                    {
                        continue;
                    }
                    foreach (XElement step in steps.Elements("li"))
                    {
                        string stepId = step.Element("id")?.Value ?? "step";
                        string stepPrefix = chapterPrefix + "_Step_" + MakeSafeKeyPart(stepId);
                        CompileTextElement(step.Element("labelKey"), stepPrefix + "_Label", language);
                        CompileTextElement(step.Element("descriptionKey"), stepPrefix + "_Description", language);
                        XElement objectives = step.Element("objectives");
                        if (objectives == null)
                        {
                            continue;
                        }
                        int objectiveIndex = 0;
                        foreach (XElement objective in objectives.Elements("li"))
                        {
                            string objectivePrefix = stepPrefix + "_Objective_" + objectiveIndex;
                            CompileTextElement(objective.Element("labelKey"), objectivePrefix + "_Label", language);
                            CompileTextElement(objective.Element("descriptionKey"), objectivePrefix + "_Description", language);
                            objectiveIndex++;
                        }
                    }
                }
            }
            return result;
        }

        private void CompileTextElement(XElement element, string key, XElement language)
        {
            if (element == null || element.Value.NullOrEmpty() || element.Value.CanTranslate())
            {
                return;
            }
            string source = element.Value;
            element.Value = key;
            if (language.Element(key) == null)
            {
                language.Add(new XElement(key, source));
            }
        }

        private string MakeTextKeyPrefix()
        {
            return "CQF_QuestBook_" + MakeSafeKeyPart(curDef.defName.NullOrEmpty() ? "Unnamed" : curDef.defName);
        }

        private static string MakeSafeKeyPart(string value)
        {
            return new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        }

        private static void PrepareEditableText(QuestBookDef book)
        {
            foreach (QuestBookChapter chapter in book.chapters)
            {
                if (chapter.labelKey.CanTranslate())
                {
                    chapter.labelKey = chapter.labelKey.Translate().ToString();
                }
                if (chapter.descriptionKey.CanTranslate())
                {
                    chapter.descriptionKey = chapter.descriptionKey.Translate().ToString();
                }
                foreach (QuestBookStep step in chapter.steps)
                {
                    if (step.labelKey.CanTranslate())
                    {
                        step.labelKey = step.labelKey.Translate().ToString();
                    }
                    if (step.descriptionKey.CanTranslate())
                    {
                        step.descriptionKey = step.descriptionKey.Translate().ToString();
                    }
                    foreach (QuestBookObjective objective in step.objectives)
                    {
                        if (objective.labelKey.CanTranslate())
                        {
                            objective.labelKey = objective.labelKey.Translate().ToString();
                        }
                        if (objective.descriptionKey.CanTranslate())
                        {
                            objective.descriptionKey = objective.descriptionKey.Translate().ToString();
                        }
                    }
                }
            }
        }

        private static QuestBookDef curDef;
        private readonly QuestBookNodeCanvas nodeCanvas = new QuestBookNodeCanvas();
        private readonly QuestBookChapterSidebar chapterSidebar = new QuestBookChapterSidebar();
    }
}
