using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditDialogResult : Window
    {
        public Dialog_EditDialogResult(QuestEditor_Dialog parent,DialogResult result, DialogOption option, DialogNode node) 
        {
            this.parent = parent;
            this.node = node;
            this.option = option;
            this.result = result;
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
            CQFEditorTools.DrawLabelAndText_Line(0f, "ResultName".Translate(), ref this.result.resultName, 0f, 300f);
            float y = 30f;
            string nextNodeText = "Null".Translate();
            if (this.result.nextIndex != null && tree.nodeMoulds.TryGetValue(this.result.nextIndex.Value, out DialogNode node))
            {
                nextNodeText = node.text;
            }
            Rect nextRect = new Rect(0f, y, 100f, 20f);
            if (Widgets.ButtonText(nextRect, "NextNode".Translate(nextNodeText), false) && this.result.nextIndex != null)
            {
                this.Close();
                if (Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogNode)) is Window window)
                {
                    window.Close();
                }
                Find.WindowStack.Add(new Dialog_EditDialogNode(tree.nodeMoulds[this.result.nextIndex.Value], this.parent));
            }
            TooltipHandler.TipRegion(nextRect, "NextNodeTip".Translate());
            nextRect.x = inRect.width - 160f;
            if (Widgets.ButtonText(nextRect, "SelectNode".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(tree.nodeMoulds, (i, n) =>
                {
                    if (n.index == 0) 
                    {
                        Messages.Message("No select inital node as next node",MessageTypeDefOf.CautionInput);
                        return;
                    }
                    tree.ChangeNextNodeToOtherNode(this.node, n, this.result);
                    this.parent.InitCurTree();
                }, (i, n) => n.text, new List<FloatMenuOption>()
                {
                    new FloatMenuOption("AddNewNode".Translate(),() =>
                {
                    tree.ChangeNextNodeToOtherNode(this.node,tree.CreateNewNode(this.node),this.result,true);
                    this.parent.InitCurTree();
                })
                ,
                    new FloatMenuOption("Null".Translate(), () =>
                {
                    tree.ChangeNextNodeToOtherNode(this.node, null, this.result);
                    this.parent.InitCurTree();
                })
                }, (i, n) => n.index != this.result.nextIndex);
            }
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, 0f, this.result.actions, inRect, "CQFActions".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            CQFEditorTools.DrawIDrawList(ref y,0f,this.result.conditions,inRect, "DialogConditions".Translate().Colorize(ColorLibrary.SkyBlue));
            Widgets.EndScrollView();
            this.height = y;
        }

        public float height = 0f;
        public DialogOption option;
        public DialogNode node;
        public QuestEditor_Dialog parent;
        public DialogResult result;
        private Vector2 scrollPosition;
    }
}
