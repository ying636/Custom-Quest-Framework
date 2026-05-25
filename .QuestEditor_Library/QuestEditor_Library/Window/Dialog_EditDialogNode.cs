using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditDialogNode : Window
    {
        public Dialog_EditDialogNode(DialogNode node,QuestEditor_Dialog dialog) 
        {
            this.node = node;
            this.parent = dialog;
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
        public override Vector2 InitialSize => Dialog_EditDialogNode.initSize;
        public override void Notify_ClickOutsideWindow()
        {
            QuickSearchWidget commonSearchWidget = this.CommonSearchWidget;
            if (commonSearchWidget == null)
            {
                return;
            }
            commonSearchWidget.Focus();
        }
        public override void PostClose()
        {
            base.PostClose();
            this.parent.InitCurTree();
        }
        public override void DoWindowContents(Rect inRect)
        {
            DialogTreeDef tree = this.parent.CurTree;  
            float y = 30f;
            float width = inRect.width - 16f;
            Widgets.BeginScrollView(new Rect(0f, 0f, width, 600f),ref this.scrollPosition,new Rect(0f, 0f, width,670f + (35f * this.node.options.Count)));
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, tree.title);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, 150f, 20f), "DialogText".Translate().Colorize(ColorLibrary.SkyBlue));
            if (Widgets.ButtonText(new Rect(inRect.width - 200f,y,150f,25f),"ExtraDialogText".Translate(),false)) 
            {
                Find.WindowStack.Add(new Dialog_EditExtraText(this.node));
            }
            this.node.text = Widgets.TextArea(new Rect(0f, y + 30f, width - 25f,200f), this.node.text);
            y += 250f;
            Widgets.Label(new Rect(0f, y, 150f, 20f),"DialogOptions".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            foreach (DialogOption option in this.node.options) 
            {
                if (Widgets.ButtonText(new Rect(5f, y, 350f, 25f), option.text,false)) 
                {
                    Find.WindowStack.Add(new Dialog_EditDialogOption(this.parent,option,this.node));
                }
                y += 35f;
            }
            if (Widgets.ButtonText(new Rect(0f,y,80f,30f),"Add".Translate())) 
            {
                this.node.options.Add(new DialogOption());
                this.parent.InitCurTree();
            }
            if (Widgets.ButtonText(new Rect(100f, y,80f, 30f), "AddSpecialOption".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(typeof(DialogOption).AllSubclassesNonAbstract(), t =>
                {
                    this.node.options.Add(Activator.CreateInstance(t)as DialogOption);
                    this.parent.InitCurTree();
                },t => t.Name.Translate());
            }
            if (Widgets.ButtonText(new Rect(200f, y,80f, 30f), "Delete".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(this.node.options,(x) =>
                {
                    this.node.options.Remove(x); 
                    this.parent.CurTree.Update();
                },x => x.text);
            }
            Widgets.EndScrollView();
        }

        public DialogNode node;
        public QuestEditor_Dialog parent;  
        private Vector2 scrollPosition;
        public static readonly Vector2 initSize = new Vector2(600f,500f);
    }
}
