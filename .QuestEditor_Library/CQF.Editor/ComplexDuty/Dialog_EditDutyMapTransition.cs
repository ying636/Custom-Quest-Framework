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

        public override Vector2 InitialSize => new Vector2(560f, 720f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(5f, 5f, 430f, 30f), "CQF_DutyMapTransitionEditor".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect view = new Rect(0f, 0f, inRect.width - 20f, this.height);
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
