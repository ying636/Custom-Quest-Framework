using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_InteractionOption : Window
    {
        public Dialog_InteractionOption(InteractionOperation operation, Thing owner = null)
        {
            this.operation = operation;
            CQFSignalEditor.InvalidateSummary(operation);
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

        public override void DoWindowContents(Rect inRect)
        {
            using (new CQFEditorContext(this.owner))
            {
                this.DrawContents(inRect);
            }
        }

        private void DrawContents(Rect inRect)
        {
            float x = 10f; 
            float y = 8f;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 210f, 30f), this.owner == null ? this.operation.interactionText : this.owner.LabelCap + " " + this.owner.Position);
            if (this.owner != null && Widgets.ButtonText(new Rect(inRect.width - 200f, 0f, 165f, 30f), "CQF_EditorCheck".Translate()))
            {
                Find.WindowStack.Add(new Dialog_CQFInteractionCheck(this.operation, this.owner));
            }
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, this.height + 12f);
            Widgets.BeginScrollView(new Rect(0f, 38f, inRect.width, inRect.height - 38f), ref this.pos, viewRect);
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
