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

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.BeginScrollView(new Rect(0f,0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width - 20f, this.height + 10f));
            float y = 0f;
            this.iDrawable.Draw(ref y,inRect,0f);
            this.height = y;
            Widgets.EndScrollView();
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        IDrawable iDrawable = null;
    }
}
