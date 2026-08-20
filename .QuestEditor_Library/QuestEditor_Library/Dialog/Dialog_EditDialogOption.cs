using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditDialogOption : Window
    {
        public Dialog_EditDialogOption(QuestEditor_Dialog parent,DialogOption option,DialogNode node) 
        {
            this.parent = parent;
            this.node = node;
            this.option = option;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.draggable = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.onlyOneOfTypeAllowed = false;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
        }
        public override void Notify_ClickOutsideWindow()
        {
        }
        public override Vector2 InitialSize => new Vector2(560f, 560f);
        public override void DoWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height, this.height + 20f));
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            this.height = this.option.Draw(viewRect, this.parent, this.node);
            Widgets.EndScrollView();
        }

        public float height = 0f;
        public DialogOption option;
        public DialogNode node;
        public QuestEditor_Dialog parent;
        private Vector2 scrollPosition;
    }
}
