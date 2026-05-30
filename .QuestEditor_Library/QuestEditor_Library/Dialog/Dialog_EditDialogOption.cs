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
        public override void DoWindowContents(Rect inRect)
        {
            DialogTreeDef tree = this.parent.CurTree;
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, new Rect(0f, 0f, inRect.width - 20f, this.height));
            this.height = this.option.Draw(inRect, parent, node);
            Widgets.EndScrollView();
 
        }

        public float height = 0f;
        public DialogOption option;
        public DialogNode node;
        public QuestEditor_Dialog parent;
        private Vector2 scrollPosition;
    }
}
