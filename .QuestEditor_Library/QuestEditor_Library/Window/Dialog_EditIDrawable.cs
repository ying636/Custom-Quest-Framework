using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditIDrawable : Window
    {
        public Dialog_EditIDrawable(IDrawable iDrawable)
        {
            this.iDrawable = iDrawable;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(this.height + 10f, inRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float y = 0f;
            this.iDrawable.Draw(ref y, viewRect, 0f);
            this.height = y;
            Widgets.EndScrollView();
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        private IDrawable iDrawable;
    }
}
