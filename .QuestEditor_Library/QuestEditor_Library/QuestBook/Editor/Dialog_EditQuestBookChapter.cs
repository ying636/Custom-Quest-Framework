using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestBookChapter : Window
    {
        public Dialog_EditQuestBookChapter(QuestBookChapter chapter)
        {
            this.chapter = chapter;
            if (chapter.labelKey.CanTranslate())
            {
                chapter.labelKey = chapter.labelKey.Translate().ToString();
            }
            if (chapter.descriptionKey.CanTranslate())
            {
                chapter.descriptionKey = chapter.descriptionKey.Translate().ToString();
            }
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(560f, 460f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            float x = 12f;
            float width = inRect.width - 36f;
            Widgets.Label(new Rect(x, y, width, 32f), "CQF_QuestBook_ChapterProperties".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 46f;
            DrawTextField(ref y, x, width, "CQF_QuestBook_ChapterDataName".Translate(), ref chapter.id);
            DrawTextField(ref y, x, width, "CQF_QuestBook_ChapterName".Translate(), ref chapter.labelKey);
            DrawTextField(ref y, x, width, "CQF_QuestBook_ChapterDescription".Translate(), ref chapter.descriptionKey);
            DrawActions(ref y, x, width, "CQF_QuestBook_UnlockActions".Translate(), chapter.onUnlockActions);
            DrawActions(ref y, x, width, "CQF_QuestBook_CompleteActions".Translate(), chapter.onCompleteActions);
        }

        private void DrawTextField(ref float y, float x, float width, string label, ref string value)
        {
            Widgets.Label(new Rect(x, y, width, 24f), label.Colorize(ColorLibrary.PaleBlue));
            y += 26f;
            value = Widgets.TextField(new Rect(x, y, width, 30f), value ?? string.Empty);
            y += 42f;
        }

        private void DrawActions(ref float y, float x, float width, string title, List<CQFAction> actions)
        {
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, actions, new Rect(0f, 0f, width, 140f), title, action => action.GetType().Name.Translate());
            y += 10f;
        }

        private readonly QuestBookChapter chapter;
    }
}
