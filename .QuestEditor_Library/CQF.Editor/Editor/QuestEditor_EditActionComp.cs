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
        public QuestEditor_EditActionComp(ActionComp comp, Thing owner = null)
        {
            this.comp = comp;
            this.owner = owner ?? CQFEditorContext.SourceThing;
            this.doCloseX = true;
            this.forcePause = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Small;
            Rect viewport = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height + 12f));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 5f;
            using (new CQFEditorContext(this.owner))
            {
                comp.Draw(ref y,content,5f);
            }
            this.height = y;
            Widgets.EndScrollView();
        }

        public float height = 0f;
        public Vector2 scrollPos = Vector2.zero;
        public ActionComp comp;
        private readonly Thing owner;
    }
}
