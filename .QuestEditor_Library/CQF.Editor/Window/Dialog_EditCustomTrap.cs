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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            float y = 10f;
            Rect viewport = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height + 12f));
            Widgets.BeginScrollView(viewport, ref this.pos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            using (new CQFEditorContext(this.trap))
            {
                this.trap.Draw(ref y,content,10f);
            }
            Widgets.EndScrollView();
            this.height = y + 5f;
        }
        public string buffer;
        public float height;
        public CustomTrap trap;
        public Vector2 pos = Vector2.zero;
    }
}
