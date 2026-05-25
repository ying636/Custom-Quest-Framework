using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_EditActionComp : Window
    {
        public QuestEditor_EditActionComp(ActionComp comp)
        {
            this.comp = comp;
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.BeginScrollView(new Rect(4f,5f, inRect.width - 10f, inRect.height - 5f), ref this.scrollPos, new Rect(0f,0f, inRect.width - 10f,this.height));
            float y = 5f;
            comp.Draw(ref y,inRect,5f);
            this.height = y;
            Widgets.EndScrollView();
        }

        public float height = 0f;
        public Vector2 scrollPos = Vector2.zero;
        public ActionComp comp;
    }
}
