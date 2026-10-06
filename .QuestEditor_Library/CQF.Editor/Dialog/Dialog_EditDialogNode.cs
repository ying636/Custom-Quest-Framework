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
            float x = 8f;
            float y = 8f;
            float width = inRect.width - 36f;
            float contentHeight = 370f + (112f * this.node.images.Count) + (40f * this.node.options.Count);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height, contentHeight));
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            this.DrawTextHeader(ref y, x, width);
            this.node.text = Widgets.TextArea(new Rect(x + 6f, y, width - 12f, 150f), this.node.text);
            y += 166f;
            this.DrawSectionHeader(ref y, x, width, "DialogImages".Translate(), "DialogImages_Tip".Translate(),
                () =>
                {
                    this.node.images.Add(new DialogImage());
                    this.parent.InitCurTree();
                },
                () => CQFEditorTools.DrawFloatMenu(this.node.images, image =>
                {
                    this.node.images.Remove(image);
                    this.parent.CurTree.Update();
                }, image => image.imagePath.NullOrEmpty() ? "DialogImage_NotSelected".Translate().ToString() : image.imagePath),
                () => this.node.images.Any());
            for (int i = 0; i < this.node.images.Count; i++)
            {
                DialogImage image = this.node.images[i];
                Texture2D texture = image.imagePath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(image.imagePath, false);
                Rect itemRect = new Rect(x + 6f, y, width - 12f, 96f);
                Widgets.DrawHighlightIfMouseover(itemRect);
                Rect imageRect = new Rect(x + 12f, y + 8f, 112f, 74f);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(imageRect, texture, 1f);
                }
                else
                {
                    Widgets.DrawBoxSolid(imageRect, Color.black);
                }
                if (Widgets.ButtonInvisible(imageRect))
                {
                    Find.WindowStack.Add(new Dialog_SelectDialogImage(path => image.imagePath = path, image.imagePath));
                }
                TooltipHandler.TipRegion(imageRect, "DialogImage_SelectTip".Translate());
                Widgets.Label(new Rect(x + 142f, y + 12f, 100f, 25f), "DialogImage_Scale".Translate());
                Widgets.TextFieldNumeric(new Rect(x + 242f, y + 8f, 80f, 28f), ref image.scale, ref image.buffer_scale);
                Widgets.Label(new Rect(x + 142f, y + 48f, width - 170f, 25f),
                    (image.imagePath.NullOrEmpty() ? "DialogImage_NotSelected".Translate().ToString() : image.imagePath).Colorize(Color.gray));
                Widgets.DrawLine(new Vector2(itemRect.x + 6f, itemRect.yMax), new Vector2(itemRect.xMax - 6f, itemRect.yMax), ColorLibrary.SkyBlue, 1f);
                y += 104f;
            }
            if (!this.node.images.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogImages".Translate());
            }
            y += 10f;
            this.DrawSectionHeader(ref y, x, width, "DialogOptions".Translate(), null,
                () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("Add".Translate(), () =>
                    {
                        this.node.options.Add(new DialogOption());
                        this.parent.InitCurTree();
                    }),
                    new FloatMenuOption("AddSpecialOption".Translate(), () =>
                    {
                        CQFEditorTools.DrawFloatMenu(typeof(DialogOption).AllSubclassesNonAbstract(), type =>
                        {
                            this.node.options.Add(Activator.CreateInstance(type) as DialogOption);
                            this.parent.InitCurTree();
                        }, type => type.Name.Translate());
                    })
                })),
                () => CQFEditorTools.DrawFloatMenu(this.node.options, option =>
                {
                    this.node.options.Remove(option);
                    this.parent.CurTree.Update();
                }, option => option.text),
                () => this.node.options.Any());
            foreach (DialogOption option in this.node.options) 
            {
                Rect optionRect = new Rect(x + 8f, y, width - 16f, 30f);
                if (Widgets.ButtonText(optionRect, option.text, false))
                {
                    Find.WindowStack.Add(new Dialog_EditDialogOption(this.parent,option,this.node));
                }
                y += 34f;
            }
            if (!this.node.options.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogOptions".Translate());
            }
            Widgets.EndScrollView();
        }

        private void DrawTextHeader(ref float y, float x, float width)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 160f, 25f);
            Widgets.Label(labelRect, "DialogText".Translate().Colorize(ColorLibrary.SkyBlue));
            Rect buttonRect = new Rect(x + width - 142f, y + 2f, 130f, 25f);
            if (Widgets.ButtonText(buttonRect, "ExtraDialogText".Translate(), false))
            {
                Find.WindowStack.Add(new Dialog_EditExtraText(this.node));
            }
            y += 40f;
        }

        private void DrawSectionHeader(ref float y, float x, float width, string label, string tip,
            Action addAction, Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(labelRect, tip);
            }
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

        private void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }

        public DialogNode node;
        public QuestEditor_Dialog parent;  
        private Vector2 scrollPosition;
        public static readonly Vector2 initSize = new Vector2(560f, 560f);
    }
}
