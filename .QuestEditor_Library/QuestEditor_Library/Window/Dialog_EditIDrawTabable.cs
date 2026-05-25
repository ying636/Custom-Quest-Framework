using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditIDrawTabable : Window
    {
        public Dialog_EditIDrawTabable(IDrawTabable iDrawable)
        {
            this.iDrawable = iDrawable;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.iDrawable.DrawTab();
        }

        IDrawTabable iDrawable = null;
    }
}
