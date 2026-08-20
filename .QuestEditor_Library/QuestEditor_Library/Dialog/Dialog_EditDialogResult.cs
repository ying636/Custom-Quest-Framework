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
        public override Vector2 InitialSize => new Vector2(560f, 520f);
        public override void DoWindowContents(Rect inRect)
        {
            DialogTreeDef tree = this.parent.CurTree;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height, this.height + 20f));
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            float x = 8f;
            float y = 8f;
            float width = viewRect.width - 18f;
            Widgets.DrawHighlight(new Rect(x + 4f, y - 2f, width - 8f, 32f));
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 16f, 25f), "DialogResults".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 40f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ResultName".Translate(), ref this.result.resultName, x + 8f, 150f);
            y += 38f;
            string nextNodeText = "Null".Translate();
            if (this.result.nextIndex != null && tree.nodeMoulds.TryGetValue(this.result.nextIndex.Value, out DialogNode node))
            {
                nextNodeText = node.text;
            }
            Rect nextRect = new Rect(x + 8f, y, width - 196f, 28f);
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
            nextRect.x = x + width - 176f;
            nextRect.width = 168f;
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
            y += 42f;
            this.DrawSectionHeader(ref y, x, width, "CQFActions".Translate(),
                () => CQFEditorTools.OpenCQFActionSelect(type => this.result.actions.Add((CQFAction)Activator.CreateInstance(type))),
                () => CQFEditorTools.DrawFloatMenu(this.result.actions, action => this.result.actions.Remove(action), action => action.GetType().Name.Translate()),
                () => this.result.actions.Any());
            foreach (CQFAction action in this.result.actions)
            {
                float itemY = y;
                action.Draw(ref y, viewRect, x + 16f);
                this.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }
            if (!this.result.actions.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogActions".Translate());
            }
            y += 10f;
            this.DrawSectionHeader(ref y, x, width, "DialogConditions".Translate(),
                () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(),
                    type => this.result.conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate()),
                () => CQFEditorTools.DrawFloatMenu(this.result.conditions, condition => this.result.conditions.Remove(condition), condition => condition.GetType().Name.Translate()),
                () => this.result.conditions.Any());
            foreach (DialogCondition condition in this.result.conditions)
            {
                float itemY = y;
                condition.Draw(ref y, viewRect, x + 16f);
                this.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }
            if (!this.result.conditions.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogConditions".Translate());
            }
            Widgets.EndScrollView();
            this.height = y + 10f;
        }

        private void DrawSectionHeader(ref float y, float x, float width, string label, Action addAction,
            Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), label.Colorize(ColorLibrary.SkyBlue));
            Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(buttonRect, TexButton.Plus))
            {
                addAction();
            }
            TooltipHandler.TipRegion(buttonRect, "Add".Translate());
            buttonRect.x += 30f;
            if (Widgets.ButtonImage(buttonRect, TexButton.Delete) && canRemove())
            {
                removeAction();
            }
            TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            y += 40f;
        }

        private void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x + 6f, startY - 2f, width - 12f, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(rect.x + 6f, rect.yMax), new Vector2(rect.xMax - 6f, rect.yMax), ColorLibrary.SkyBlue, 1f);
        }

        private void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }

        public float height = 0f;
        public DialogOption option;
        public DialogNode node;
        public QuestEditor_Dialog parent;
        public DialogResult result;
        private Vector2 scrollPosition;
    }
}
