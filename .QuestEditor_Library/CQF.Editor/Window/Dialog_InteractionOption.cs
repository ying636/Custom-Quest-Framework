using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_InteractionOption : Window, ICQFAIEditorHost
    {
        public Dialog_InteractionOption(InteractionOperation operation, Thing owner = null)
        {
            this.operation = operation;
            this.owner = owner ?? CQFEditorContext.SourceThing;
            this.windowRect = new Rect((UI.screenWidth - 760f) / 2f, (UI.screenHeight - 680f) / 2f, 760f, 680f);
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }
        public override Vector2 InitialSize => new Vector2(760f, 680f);
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(nameof(InteractionOperation), () => this.operation, value =>
        {
            foreach (var field in new CQFAIModel().Fields(typeof(InteractionOperation))) field.SetValue(this.operation, field.GetValue(value));
        }, isValid: () => Find.WindowStack.Windows.Contains(this) && (this.owner == null || !this.owner.Destroyed), owner: this);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            using (new CQFEditorContext(this.owner))
            {
                this.DrawContents(inRect);
            }
        }

        private void DrawContents(Rect inRect)
        {
            float x = 10f;
            float y = 8f;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), this.owner == null ? this.operation.interactionText : this.owner.LabelCap + " " + this.owner.Position);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height - 38f, this.height + 12f));
            Widgets.BeginScrollView(new Rect(0f, 38f, inRect.width, inRect.height - 38f), ref this.pos, viewRect);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(viewRect.width);
            this.operation.Draw(ref y,viewRect,x);
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        public string buffer;
        public float height;
        public InteractionOperation operation;
        public Vector2 pos = Vector2.zero;
        private readonly Thing owner;
    }
}
