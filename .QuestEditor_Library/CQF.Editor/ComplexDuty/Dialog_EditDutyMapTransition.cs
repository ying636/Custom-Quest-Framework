using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditDutyMapTransition : Window
    {
        public Dialog_EditDutyMapTransition(DutyMapTransition transition)
        {
            this.transition = transition;
            this.doCloseX = true;
            this.draggable = true;
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(560f, UI.screenWidth - 24f), Mathf.Min(720f, UI.screenHeight - 24f));

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(5f, 5f, inRect.width - 38f, 30f), "CQF_DutyMapTransitionEditor".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect view = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(this.height, inRect.height - 45f));
            Widgets.BeginScrollView(new Rect(0f, 40f, inRect.width, inRect.height - 45f), ref this.scrollPos, view);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(view.width);
            float y = 5f;
            this.transition.Draw(ref y, view, 5f);
            Widgets.EndScrollView();
            this.height = y + 40f;
        }

        private readonly DutyMapTransition transition;
        private Vector2 scrollPos = Vector2.zero;
        private float height = 660f;
    }
}
