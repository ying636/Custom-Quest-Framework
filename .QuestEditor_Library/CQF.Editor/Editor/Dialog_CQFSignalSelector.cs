using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFSignalSelector : Window
    {
        public Dialog_CQFSignalSelector(object? owner = null, Action<string, bool, bool>? selected = null, Map? map = null, CustomMapDataDef? definition = null)
        {
            this.owner = owner;
            this.selected = selected;
            this.map = map;
            this.definition = definition;
            this.doCloseX = true;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.catalog = CQFSignalCatalog.Build(map, definition);
            this.RefreshRows();
        }

        public override Vector2 InitialSize => new Vector2(860f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 32f), "CQF_MapSignals_Title".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 38f, inRect.width, 44f), "CQF_MapSignals_KnownOnly".Translate().Colorize(Color.gray));
            Widgets.Label(new Rect(0f, 82f, inRect.width, 30f), this.connectionStatus);
            string search = Widgets.TextField(new Rect(0f, 116f, inRect.width - 110f, 30f), this.search);
            if (search != this.search)
            {
                this.search = search;
                this.RefreshRows();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 102f, 116f, 102f, 30f), "CQF_MapSignals_Refresh".Translate()))
            {
                this.catalog = CQFSignalCatalog.Build(this.map, this.definition);
                this.RefreshRows();
            }
            Rect outRect = new Rect(0f, 156f, inRect.width, inRect.height - 156f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.rows.Count * 110f));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            if (this.rows.Count == 0)
            {
                Widgets.Label(new Rect(8f, 8f, viewRect.width - 16f, 42f), "CQF_MapSignals_None".Translate());
            }
            for (int index = 0; index < this.rows.Count; index++)
            {
                float y = index * 110f;
                if (y + 106f < this.scroll.y || y > this.scroll.y + outRect.height)
                {
                    continue;
                }
                this.DrawRow(new Rect(0f, y, viewRect.width, 104f), this.rows[index]);
            }
            Widgets.EndScrollView();
        }

        private void RefreshRows()
        {
            CQFSignalEndpoint? own = this.owner == null ? null : this.catalog.FindOwner(this.owner);
            if (this.owner == null)
            {
                this.connectionStatus = string.Empty;
            }
            else if (own == null)
            {
                this.connectionStatus = "CQF_MapSignals_ContextUnknown".Translate();
            }
            else
            {
                int connections = this.catalog.Entries.Count(entry => entry.IsReceiver != own.IsReceiver && !ReferenceEquals(entry.Owner, this.owner) && entry.Matches(own));
                bool mismatched = this.catalog.Entries.Any(entry => entry.IsReceiver != own.IsReceiver && entry.Signal == own.Signal && !entry.Matches(own));
                this.connectionStatus = connections > 0 ? "CQF_MapSignals_Connected".Translate() + ": " + connections
                    : (mismatched ? "CQF_MapSignals_ScopeMismatch" : "CQF_MapSignals_Unlinked").Translate().ToString();
            }
            this.rows = this.catalog.Entries.Where(entry => !entry.Signal.NullOrEmpty()
                && (this.search.NullOrEmpty() || (entry.DisplaySignal + " " + entry.SourceLabel + " " + entry.ScopeLabel)
                    .IndexOf(this.search, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(entry => entry.Signal).ThenBy(entry => entry.IsReceiver).ThenBy(entry => entry.SourceLabel).ToList();
        }

        private void DrawRow(Rect rect, CQFSignalEndpoint entry)
        {
            Widgets.DrawMenuSection(rect);
            float contentWidth = rect.width - 120f;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 6f, contentWidth, 25f), entry.DisplaySignal.Colorize(ColorLibrary.PaleBlue));
            string direction = (entry.IsReceiver ? "InSignal" : "OutSignal").Translate();
            string kind = entry.IsAutomatic ? " / " + "CQF_MapSignals_Automatic".Translate() : "";
            string template = entry.IsTemplate ? " / " + "CQF_MapSignals_Template".Translate() : "";
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 32f, contentWidth, 24f), direction + " " + entry.ScopeLabel + kind + template);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 58f, contentWidth, 38f), entry.SourceLabel.Colorize(Color.gray));
            TooltipHandler.TipRegion(new Rect(rect.x, rect.y, contentWidth, rect.height), entry.DisplaySignal + "\n" + entry.SourceLabel);
            if (entry.CanLocate && Widgets.ButtonText(new Rect(rect.xMax - 105f, rect.y + 58f, 95f, 30f), "CQF_MapSignals_Locate".Translate()))
            {
                entry.Locate();
                this.Close();
            }
            if (this.selected != null)
            {
                bool canSelect = this.TrySelection(entry, out string signal, out bool part, out bool quest);
                Rect selectRect = new Rect(rect.xMax - 105f, rect.y + 12f, 95f, 30f);
                if (Widgets.ButtonText(selectRect, "Select".Translate(), active: canSelect))
                {
                    this.selected(signal, part, quest);
                    this.Close();
                }
                if (!canSelect)
                {
                    TooltipHandler.TipRegion(selectRect, "CQF_MapSignals_IncompatibleScope".Translate());
                }
            }
        }

        private bool TrySelection(CQFSignalEndpoint entry, out string signal, out bool part, out bool quest)
        {
            signal = entry.Signal;
            part = entry.PartScoped;
            quest = entry.QuestScoped;
            bool receiver = this.owner is TrapComp || this.owner is ActionComp;
            if (!entry.Selectable || entry.IsReceiver == receiver || ReferenceEquals(entry.Owner, this.owner))
            {
                return false;
            }
            CQFSignalEndpoint? own = this.owner == null ? null : this.catalog.FindOwner(this.owner);
            if (own != null && own.IsTemplate != entry.IsTemplate)
            {
                return false;
            }
            if (own?.IsTemplate == true && (!own.CanChangePartScope && own.PartScoped != entry.PartScoped
                || entry.PartScoped && !own.PartName.NullOrEmpty() && !entry.PartName.NullOrEmpty() && own.PartName != entry.PartName))
            {
                return false;
            }
            if (receiver)
            {
                return !entry.IsTemplate || entry.QuestScoped;
            }
            if (own?.IsTemplate == true && !own.Prefix.NullOrEmpty())
            {
                if (!signal.StartsWith(own.Prefix, StringComparison.Ordinal))
                {
                    return false;
                }
                signal = signal.Substring(own.Prefix.Length);
            }
            else if (own?.IsTemplate == false)
            {
                if (!own.Prefix.NullOrEmpty() && signal.StartsWith(own.Prefix, StringComparison.Ordinal))
                {
                    signal = signal.Substring(own.Prefix.Length);
                    quest = true;
                }
                else
                {
                    quest = false;
                }
            }
            return true;
        }

        private readonly object? owner;
        private readonly Action<string, bool, bool>? selected;
        private readonly Map? map;
        private readonly CustomMapDataDef? definition;
        private CQFSignalCatalog catalog;
        private List<CQFSignalEndpoint> rows = new List<CQFSignalEndpoint>();
        private string search = string.Empty;
        private string connectionStatus = string.Empty;
        private Vector2 scroll;
    }
}
