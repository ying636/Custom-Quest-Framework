using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditExtraText : Window
    {
        public Dialog_EditExtraText(DialogNode node)
        {
            this.node = node;
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
        public override void DoWindowContents(Rect inRect)
        {
            float width = inRect.width - 16f;
            Widgets.BeginScrollView(new Rect(0f, 0f, width, 600f), ref this.scrollPosition, new Rect(0f, 0f, width, 670f + (35f * this.node.options.Count)));
            float y = 5f;
            CQFEditorTools.DrawEditableStringList(this.node.extraText,ref y, "ExtraDialogText".Translate().Colorize(ColorLibrary.SkyBlue));
            Widgets.EndScrollView();
        }

        public DialogNode node;
        private Vector2 scrollPosition;
        public static readonly Vector2 initSize = new Vector2(600f, 500f);
    }
}
