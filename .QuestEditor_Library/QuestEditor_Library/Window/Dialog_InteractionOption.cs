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
        public Dialog_InteractionOption(InteractionOperation operation)
        {
            this.operation = operation;
            this.windowRect = new Rect((UI.screenWidth - 760f) / 2f, (UI.screenHeight - 680f) / 2f, 760f, 680f);
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false; 
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float x = 10f; 
            float y = 8f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width - 20f, this.height + 12f));
            this.operation.Draw(ref y,inRect,x);
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        public string buffer;
        public float height;
        public InteractionOperation operation;
        public Vector2 pos = Vector2.zero;
    }
}
