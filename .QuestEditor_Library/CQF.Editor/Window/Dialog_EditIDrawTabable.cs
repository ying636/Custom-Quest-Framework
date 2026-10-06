using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditIDrawTabable : Window, ICQFAIEditorHost
    {
        public Dialog_EditIDrawTabable(IDrawTabable iDrawable)
        {
            this.iDrawable = iDrawable;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(580f, 650f);
        public CQFAIEditorContext? AIContext => this.iDrawable is Thing thing ? CQFAIThingContext.Create(thing, this) : null;

        public override void DoWindowContents(Rect inRect)
        {
            using (new CQFEditorContext(this.iDrawable as Thing))
            {
                this.iDrawable.DrawTab();
            }
        }

        private readonly IDrawTabable iDrawable;
    }
}
