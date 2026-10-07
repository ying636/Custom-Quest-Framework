using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditIDrawable : Window, ICQFAIEditorHost
    {
        public Dialog_EditIDrawable(IDrawable iDrawable, QuestBookDef questBook = null)
        {
            this.iDrawable = iDrawable;
            this.owner = CQFEditorContext.SourceThing;
            this.questBook = questBook;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.draggable = true;
            this.resizeable = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 620f);
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(this.iDrawable.GetType().Name, () => this.iDrawable, value =>
        {
            foreach (System.Reflection.FieldInfo field in value.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                if (!field.IsInitOnly) field.SetValue(this.iDrawable, field.GetValue(value));
        }, isValid: () => Find.WindowStack.Windows.Contains(this), owner: this);

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(420f, UI.screenWidth - 20f), UI.screenWidth - 20f);
            windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(300f, UI.screenHeight - 20f), UI.screenHeight - 20f);
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 28f), this.iDrawable.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            float top = 36f;
            if (this.iDrawable is PawnSpawnData pawnData)
            {
                pawnData.DrawName(ref top, 0f, new Rect(0f, top, inRect.width, 36f));
                top += 4f;
            }
            Rect outRect = new Rect(0f, top, inRect.width, inRect.height - top);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(100f, inRect.width - 20f), Mathf.Max(this.height + 10f, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float y = 0f;
            if (questBook != null && iDrawable is CQFAction_QuestBookStep questBookStepAction)
            {
                questBookStepAction.SetEditorBook(questBook);
            }
            using (new CQFEditorContext(this.owner, this.iDrawable as PawnSpawnData))
            {
                using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
                this.iDrawable.Draw(ref y, viewRect, 0f);
            }
            this.height = y;
            Widgets.EndScrollView();
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        private IDrawable iDrawable;
        private readonly QuestBookDef questBook;
        private readonly Thing owner;
    }
}
