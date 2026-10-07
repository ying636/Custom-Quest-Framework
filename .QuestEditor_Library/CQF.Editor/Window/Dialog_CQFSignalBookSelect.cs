using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFSignalBookSelect : Window
    {
        public Dialog_CQFSignalBookSelect(string? value, Action<string> selected, bool allowEmpty = true)
        {
            this.value = value ?? string.Empty;
            this.selected = selected;
            this.allowEmpty = allowEmpty;
            this.doCloseX = true;
            this.draggable = true;
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.RefreshRows();
        }

        public override Vector2 InitialSize => new Vector2(480f, 460f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 76f, 28f), "CQF_SignalBook_Title".Translate().Colorize(CQFUIStyle.Accent));
            if (CQFUIStyle.ButtonImage(new Rect(inRect.width - 66f, 0f, 28f, 28f), TexButton.Rename, tooltip: "CQF_SignalBook_OpenDescription".Translate()))
                CQFSignalEditor.OpenBook();
            Widgets.Label(new Rect(0f, 42f, 80f, 28f), "CQF_SignalBook_Search".Translate());
            string search = Widgets.TextField(new Rect(88f, 38f, inRect.width - 88f, 32f), this.search);
            if (search != this.search || this.revision != CQFSignalBook.Revision)
            {
                this.search = search;
                this.RefreshRows();
            }
            Rect viewport = new Rect(0f, 80f, inRect.width, Mathf.Max(40f, inRect.height - 172f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.rows.Count * 38f));
            string? selection = null;
            Widgets.BeginScrollView(viewport, ref this.scroll, content);
            using (new CQFUIScope(content.width, content.height))
            {
                if (this.rows.Count == 0)
                    Widgets.Label(new Rect(8f, 8f, content.width - 16f, 32f), (CQFSignalBook.Signals.Count == 0 ? "CQF_SignalBook_NoSignals" : "CQF_SignalBook_NoMatches").Translate().Colorize(CQFUIStyle.Muted));
                for (int index = 0; index < this.rows.Count; index++)
                {
                    float y = index * 38f;
                    if (y + 32f < this.scroll.y || y > this.scroll.y + viewport.height) continue;
                    Rect row = new Rect(0f, y, content.width, 32f);
                    if (CQFUIStyle.ButtonText(row, this.rows[index], overrideTextAnchor: TextAnchor.MiddleLeft)) selection = this.rows[index];
                    TooltipHandler.TipRegion(row, this.rows[index]);
                }
            }
            Widgets.EndScrollView();
            Widgets.Label(new Rect(0f, inRect.height - 76f, 80f, 28f), "Signal".Translate());
            Rect input = new Rect(88f, inRect.height - 80f, inRect.width - 88f, 32f);
            this.value = Widgets.TextField(input, this.value);
            TooltipHandler.TipRegion(input, "CQF_SignalBook_ManualInput".Translate());
            float buttonWidth = this.allowEmpty ? (inRect.width - 8f) / 2f : inRect.width;
            if (CQFUIStyle.ButtonText(new Rect(0f, inRect.height - 38f, buttonWidth, 32f), "Select".Translate(), active: this.allowEmpty || !string.IsNullOrWhiteSpace(this.value))) selection = this.value;
            if (this.allowEmpty && CQFUIStyle.ButtonText(new Rect(buttonWidth + 8f, inRect.height - 38f, buttonWidth, 32f), "CQF_SignalBook_ClearSelection".Translate())) selection = string.Empty;
            if (selection != null) this.Select(selection);
        }

        private void RefreshRows()
        {
            this.rows = CQFSignalBook.Signals.Where(signal => signal.IndexOf(this.search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            this.revision = CQFSignalBook.Revision;
            this.scroll = Vector2.zero;
        }

        private void Select(string signal)
        {
            try
            {
                this.selected(signal);
                this.Close();
            }
            catch (Exception exception)
            {
                Log.Error("[CQF] Signal selection failed: " + exception);
                string detail = exception.Message.CanTranslate() ? exception.Message.Translate().ToString() : exception.Message;
                Messages.Message("CQF_SignalBook_SelectionFailed".Translate() + ": " + detail, MessageTypeDefOf.RejectInput, false);
            }
        }

        private readonly Action<string> selected;
        private readonly bool allowEmpty;
        private List<string> rows = new List<string>();
        private string search = string.Empty;
        private string value;
        private int revision;
        private Vector2 scroll;
    }
}
