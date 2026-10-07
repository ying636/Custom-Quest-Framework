using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_DialogTreeMisc : Window
    {
        public QuestEditor_DialogTreeMisc(DialogTreeDef def, Action? changed = null)
        {
            this.def = def;
            this.changed = changed;
            this.doCloseX = true;
            this.optionalTitle = "CQF_DialogGraph_TreeSettings".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.draggable = true;
            this.resizeable = true;
        }
        public override Vector2 InitialSize => new Vector2(620f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Rect view = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 46f);
            Rect content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, this.height));
            Widgets.BeginScrollView(view, ref this.scroll, content);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(content.width);
            try
            {
                float width = content.width - 24f, y = 12f;
                this.Field(ref this.def.defName, "TreeDefName", ref y, width);
                this.Field(ref this.def.title, "DialogTitle", ref y, width);
                this.Field(ref this.def.dialogReportKey, "DialogReportKey", ref y, width, "DialogReportKeyTip");
                Rect checkbox = new Rect(12f, y, width, 32f);
                CQFUIStyle.DrawBackground(checkbox);
                Widgets.CheckboxLabeled(checkbox.ContractedBy(8f, 3f), "RequireNonHostile".Translate(), ref this.def.requireNonHostile);
                y += 48f;
                Rect header = new Rect(12f, y, width, 34f);
                CQFUIStyle.DrawMenuSection(header);
                Widgets.Label(new Rect(20f, y + 5f, width - 44f, 25f), "ExtraThingRefer".Translate().Colorize(CQFUIStyle.Accent));
                if (CQFAIIconButton.DrawImage(new Rect(header.xMax - 30f, y + 3f, 28f, 28f), TexButton.Plus, "Add".Translate())) this.def.extraThingRefers.Add(string.Empty);
                y += 44f;
                for (int index = 0; index < this.def.extraThingRefers.Count; index++)
                {
                    this.def.extraThingRefers[index] = Widgets.TextField(new Rect(12f, y, width - 38f, 32f), this.def.extraThingRefers[index] ?? string.Empty);
                    if (CQFAIIconButton.DrawImage(new Rect(12f + width - 30f, y + 2f, 28f, 28f), TexButton.Delete, "Remove".Translate()))
                    { this.def.extraThingRefers.RemoveAt(index); index--; }
                    y += 40f;
                }
                this.height = y + 12f;
            }
            finally { Widgets.EndScrollView(); }
            if (CQFAIIconButton.DrawText(new Rect(inRect.xMax - 100f, inRect.yMax - 34f, 100f, 32f), "Close".Translate())) this.Close();
        }

        public override void PostClose()
        {
            this.changed?.Invoke();
            base.PostClose();
        }

        private void Field(ref string value, string key, ref float y, float width, string? tip = null)
        {
            Rect label = new Rect(12f, y, width, 24f);
            Widgets.Label(label, key.Translate().Colorize(CQFUIStyle.Accent));
            if (tip != null) TooltipHandler.TipRegion(label, tip.Translate());
            value = Widgets.TextField(new Rect(12f, y + 28f, width, 32f), value ?? string.Empty);
            y += 74f;
        }

        public DialogTreeDef def;
        private readonly Action? changed;
        private Vector2 scroll;
        private float height = 400f;
    }
}
