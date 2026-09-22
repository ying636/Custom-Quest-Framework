using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_CQFTargetHighlight : Window
    {
        public Window_CQFTargetHighlight(TargetInfo target)
        {
            this.target = target;
            this.until = Time.realtimeSinceStartup + 3f;
            this.layer = WindowLayer.GameUI;
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(380f, 55f);

        public override void PreOpen()
        {
            base.PreOpen();
            this.windowRect.x = (UI.screenWidth - this.windowRect.width) / 2f;
            this.windowRect.y = 8f;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (Time.realtimeSinceStartup >= this.until || !this.target.IsValid || this.target.ThingDestroyed)
            {
                this.Close(false);
                return;
            }
            if (this.target.Map == Find.CurrentMap)
            {
                GenDraw.DrawTargetHighlight((LocalTargetInfo)this.target);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width - 25f, inRect.height), this.target.Label + " " + this.target.Cell);
        }

        private readonly TargetInfo target;
        private readonly float until;
    }
}
