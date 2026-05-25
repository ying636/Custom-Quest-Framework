using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditCustomTrap : Window
    {
        public Dialog_EditCustomTrap(CustomTrap trap) 
        {
            this.trap = trap; 
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            this.trap.Draw(ref y,inRect,10f);
            Widgets.EndScrollView();
            this.height = y + 5f;
        }
        public string buffer;
        public float height;
        public CustomTrap trap;
        public Vector2 pos = Vector2.zero;
    }
}
