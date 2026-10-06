using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_DialogTreeMisc : Window
    {
        public QuestEditor_DialogTreeMisc(DialogTreeDef def) 
        {
            this.def = def;
            this.doCloseX = true;
            this.optionalTitle = "CQF_DialogGraph_TreeSettings".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
        }
        public override Vector2 InitialSize => new Vector2(640f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = 5f;
            Widgets.Label(new Rect(0f, y, 130f, 28f), "TreeDefName".Translate());
            this.def.defName = Widgets.TextField(new Rect(140f, y, inRect.width - 140f, 28f), this.def.defName ?? string.Empty);
            y += 36f;
            Widgets.Label(new Rect(0f, y, 130f, 28f), "DialogTitle".Translate());
            this.def.title = Widgets.TextField(new Rect(140f, y, inRect.width - 140f, 28f), this.def.title ?? string.Empty);
            y += 40f;
            Widgets.CheckboxLabeled(new Rect(0f, y, 200f, 25f), "RequireNonHostile".Translate(), ref this.def.requireNonHostile);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "DialogReportKey".Translate(), ref this.def.dialogReportKey, 0f, 100f);
            Rect tip = new Rect(0f, y, 100f, 20f);
            if (Mouse.IsOver(tip))
            {
                TooltipHandler.TipRegion(tip, "DialogReportKeyTip".Translate());
            }
            y += 30f;
            CQFEditorTools.DrawEditableStringList(this.def.extraThingRefers,ref y,"ExtraThingRefer".Translate());
        }

        public DialogTreeDef def;
    }
}
