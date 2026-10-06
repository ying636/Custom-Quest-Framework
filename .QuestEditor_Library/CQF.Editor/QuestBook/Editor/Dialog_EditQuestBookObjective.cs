using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestBookObjective : Window
    {
        public Dialog_EditQuestBookObjective(QuestBookObjective objective)
        {
            this.objective = objective;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            if (objective == null)
            {
                Close();
                return;
            }
            Text.Font = GameFont.Small;
            Rect scrollRect = new Rect(0f, 0f, inRect.width, inRect.height);
            float viewHeight = Mathf.Max(inRect.height, contentHeight);
            Rect contentRect = new Rect(0f, 0f, inRect.width - 18f, viewHeight);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            float y = 8f;
            objective.Draw(ref y, contentRect, 8f);
            Widgets.EndScrollView();
            contentHeight = Mathf.Max(inRect.height, y + 8f);
        }

        private readonly QuestBookObjective objective;
        private Vector2 scrollPosition;
        private float contentHeight;
    }
}
