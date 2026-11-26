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
        public Dialog_EditIDrawable(IDrawable iDrawable, QuestBookDef? questBook = null)
        {
            this.iDrawable = iDrawable;
            this.owner = CQFEditorContext.SourceThing;
            this.symmetrySession = new CQFSymmetryEditSession(this.owner);
            this.questBook = questBook;
            this.forcePause = true;
            this.absorbInputAroundWindow = iDrawable is PawnSpawnData;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.draggable = true;
            this.resizeable = true;
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(620f, UI.screenWidth - 20f), Mathf.Min(620f, UI.screenHeight - 20f));
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
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, UI.screenWidth - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, UI.screenHeight - windowRect.height);
        }

        public override void PostClose()
        {
            base.PostClose();
            this.symmetrySession.Complete();
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 28f), (this.iDrawable is TrapComp ? "TrapComps".Translate() : this.iDrawable.GetType().Name.Translate()).Colorize(CQFUIStyle.Accent));
            float top = 36f;
            float footer = 0f;
            if (this.iDrawable is PawnSpawnData pawnData)
            {
                pawnData.DrawName(ref top, 0f, new Rect(0f, top, inRect.width, 36f));
                using (new CQFEditorContext(this.owner))
                    pawnData.DrawMapGroup(ref top, 0f);
                footer = 34f;
            }
            Rect outRect = new Rect(0f, top, inRect.width, Mathf.Max(0f, inRect.height - top - footer));
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, inRect.width - 20f), Mathf.Max(this.height + 10f, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float y = 0f;
            if (questBook != null && iDrawable is CQFAction_QuestBookStep questBookStepAction)
            {
                questBookStepAction.SetEditorBook(questBook);
            }
            using (new CQFEditorContext(this.owner, this.iDrawable as PawnSpawnData))
            {
                using CQFUIScope contentScope = new CQFUIScope(viewRect.width, outRect.height);
                this.iDrawable.Draw(ref y, viewRect, 0f);
            }
            this.height = y;
            Widgets.EndScrollView();
            if (this.iDrawable is PawnSpawnData recipe)
            {
                string collect = "CQF_EntityBook_Collect".Translate();
                string book = "CQF_EntityBook_Title".Translate();
                float available = Mathf.Max(0f, inRect.width - 22f);
                float bookWidth = Mathf.Min(Text.CalcSize(book).x + 20f, available * 0.38f);
                float collectWidth = Mathf.Min(Text.CalcSize(collect).x + 20f, Mathf.Max(0f, available - bookWidth - 8f));
                Rect bookRect = new(available - bookWidth, inRect.height - 29f, bookWidth, 27f);
                Rect collectRect = new(bookRect.x - 8f - collectWidth, bookRect.y, collectWidth, bookRect.height);
                if (CQFUIStyle.ButtonText(collectRect, collect, false))
                    CQFEntityBookBridge.Collect(recipe);
                if (CQFUIStyle.ButtonText(bookRect, book, false))
                {
                    CQFEntityBookCatalog.Track(recipe);
                    Map map = this.owner?.Map ?? Find.CurrentMap;
                    CQFEntityBookBridge.OpenReferences(quest: map == null ? null : GameTools.GetQuestFromMap(map));
                }
            }
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        private IDrawable iDrawable;
        private readonly QuestBookDef? questBook;
        private readonly Thing owner;
        private readonly CQFSymmetryEditSession symmetrySession;
    }
}
