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
            float x = 8f; 
            float y = 5f;
            Widgets.BeginScrollView(new Rect(0f,0f,inRect.width,inRect.height), ref this.pos,new Rect(0f,0f,inRect.width,this.height + 10f));
            Widgets.DrawBox(new Rect(0f, 3f, inRect.width - 20f, this.height), 1, QuestEditor_Dialog.blueTex);
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
