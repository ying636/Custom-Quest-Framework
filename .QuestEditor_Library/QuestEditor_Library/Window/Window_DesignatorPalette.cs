using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class Window_DesignatorPalette<T> : Window where T : class
    {
        protected Window_DesignatorPalette()
        {
            this.layer = WindowLayer.GameUI;
            this.closeOnAccept = false;
            this.closeOnCancel = true;
            this.doCloseX = false;
            this.draggable = true;
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
        }

        public override Vector2 InitialSize => new Vector2(200f, 150f);

        protected override float Margin => 6f;

        protected abstract string PaletteTitle { get; }

        protected abstract IReadOnlyList<T> AllItems { get; }

        protected abstract IReadOnlyList<T> RecentItems { get; }

        public override void PreOpen()
        {
            base.PreOpen();
            this.RefreshFilteredItems();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            try
            {
                this.DrawPaletteContents(inRect);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
            }
        }

        protected abstract string GetLabel(T item);

        protected abstract string GetTip(T item);

        protected abstract void DrawIcon(T item, Rect rect);

        protected abstract void SelectItem(T item);

        protected abstract bool IsSelected(T item);

        private void DrawPaletteContents(Rect inRect)
        {
            float contentWidth = inRect.width - ToolbarWidth;
            float y = 0f;
            Rect recentRect = new Rect(0f, y, contentWidth, RecentSectionHeight);
            Widgets.DrawMenuSection(recentRect);
            this.DrawRecentItems(recentRect.ContractedBy(SectionPadding));
            Rect closeRect = new Rect(contentWidth + ToolbarGap, 1f, ToolbarButtonSize, ToolbarButtonSize);
            if (Widgets.ButtonImage(closeRect, TexButton.CloseXSmall, true, "CloseButton".Translate()))
            {
                this.Close();
            }
            Rect pinRect = new Rect(contentWidth + ToolbarGap, 1f + ToolbarButtonSize + ToolbarGap, ToolbarButtonSize, ToolbarButtonSize);
            if (this.windowPinned)
            {
                Widgets.DrawHighlightSelected(pinRect);
            }
            string pinTip = (this.windowPinned ? "CQF_UnpinWindow" : "CQF_PinWindow").Translate();
            Texture2D pinIcon = this.windowPinned ? TexCommand.ForbidOff : TexCommand.ForbidOn;
            if (Widgets.ButtonImage(pinRect, pinIcon, true, pinTip))
            {
                this.windowPinned = !this.windowPinned;
                this.draggable = !this.windowPinned;
            }
            y = recentRect.yMax + SectionGap;

            GUI.DrawTexture(new Rect(0f, y + 1f, SearchIconSize, SearchIconSize), TexButton.Search);
            string newSearchTerms = Widgets.TextField(new Rect(SearchIconSize + SearchGap, y, contentWidth - SearchIconSize - SearchGap, SearchHeight), this.searchTerms);
            if (newSearchTerms != this.searchTerms)
            {
                this.searchTerms = newSearchTerms;
                this.RefreshFilteredItems();
            }
            y += SearchHeight + SectionGap;

            Rect selectionRect = new Rect(0f, y, contentWidth, inRect.height - y);
            Widgets.DrawMenuSection(selectionRect);
            this.DrawAllItems(selectionRect.ContractedBy(SectionPadding));
        }

        private void DrawRecentItems(Rect rect)
        {
            IReadOnlyList<T> recentItems = this.RecentItems;
            if (recentItems.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "CQF_NoRecentSelections".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            int columnCount = Math.Min(RecentColumnCount, recentItems.Count);
            float rowWidth = columnCount * RecentItemSize + (columnCount - 1) * RecentItemGap;
            float startX = rect.x + Math.Max(0f, (rect.width - rowWidth) / 2f);
            for (int index = 0; index < recentItems.Count; index++)
            {
                int column = index % RecentColumnCount;
                int row = index / RecentColumnCount;
                Rect itemRect = new Rect(
                    startX + column * (RecentItemSize + RecentItemGap),
                    rect.y + row * (RecentItemSize + RecentItemGap),
                    RecentItemSize,
                    RecentItemSize);
                this.DrawItem(recentItems[index], itemRect, false);
            }
        }

        private void DrawAllItems(Rect outRect)
        {
            int columnCount = Math.Max(1, Mathf.FloorToInt((outRect.width - ScrollbarWidth + ItemGap) / (ItemWidth + ItemGap)));
            int rowCount = Mathf.CeilToInt((float)this.filteredItems.Count / columnCount);
            float contentHeight = Math.Max(outRect.height, rowCount * (ItemHeight + ItemGap));
            Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, contentHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float rowStride = ItemHeight + ItemGap;
            int firstVisibleRow = Math.Max(0, Mathf.FloorToInt(this.scrollPosition.y / rowStride) - 1);
            int lastVisibleRow = Math.Min(rowCount - 1, Mathf.CeilToInt((this.scrollPosition.y + outRect.height) / rowStride) + 1);
            int firstVisibleIndex = firstVisibleRow * columnCount;
            int lastVisibleIndex = Math.Min(this.filteredItems.Count, (lastVisibleRow + 1) * columnCount);
            for (int index = firstVisibleIndex; index < lastVisibleIndex; index++)
            {
                int column = index % columnCount;
                int row = index / columnCount;
                Rect itemRect = new Rect(column * (ItemWidth + ItemGap), row * (ItemHeight + ItemGap), ItemWidth, ItemHeight);
                this.DrawItem(this.filteredItems[index], itemRect, false);
            }
            Widgets.EndScrollView();
        }

        private void DrawItem(T item, Rect rect, bool drawLabel)
        {
            if (this.IsSelected(item))
            {
                Widgets.DrawHighlightSelected(rect);
            }
            Widgets.DrawHighlightIfMouseover(rect);
            Rect iconRect = drawLabel
                ? new Rect(rect.x + (rect.width - IconSize) / 2f, rect.y + IconPadding, IconSize, IconSize)
                : rect.ContractedBy(RecentIconPadding);
            this.DrawIcon(item, iconRect);
            if (drawLabel)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.LabelFit(new Rect(rect.x + LabelPadding, iconRect.yMax + LabelGap, rect.width - LabelPadding * 2f, LabelHeight), this.GetCachedLabel(item));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (Widgets.ButtonInvisible(rect))
            {
                this.SelectItem(item);
            }
            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, this.GetCachedTip(item));
            }
        }

        private void RefreshFilteredItems()
        {
            IEnumerable<T> items = this.AllItems;
            if (!this.searchTerms.NullOrEmpty())
            {
                items = items.Where(item => this.GetCachedLabel(item).IndexOf(this.searchTerms, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            this.filteredItems = items.ToList();
            this.scrollPosition = Vector2.zero;
        }

        private string GetCachedLabel(T item)
        {
            if (!this.labelCache.TryGetValue(item, out string label))
            {
                label = this.GetLabel(item) ?? "";
                this.labelCache[item] = label;
            }
            return label;
        }

        private string GetCachedTip(T item)
        {
            if (!this.tipCache.TryGetValue(item, out string tip))
            {
                tip = this.GetTip(item) ?? this.GetCachedLabel(item);
                this.tipCache[item] = tip;
            }
            return tip;
        }

        private const float ToolbarButtonSize = 18f;
        private const float IconPadding = 2f;
        private const float IconSize = 28f;
        private const float ItemGap = 3f;
        private const float ItemHeight = 30f;
        private const float ItemWidth = 30f;
        private const float LabelGap = 1f;
        private const float LabelHeight = 20f;
        private const float LabelPadding = 2f;
        private const int RecentColumnCount = 5;
        private const float RecentIconPadding = 2f;
        private const float RecentItemGap = 3f;
        private const float RecentItemSize = 28f;
        private const float RecentSectionHeight = 34f;
        private const float ScrollbarWidth = 16f;
        private const float SearchGap = 3f;
        private const float SearchHeight = 20f;
        private const float SearchIconSize = 18f;
        private const float SectionGap = 3f;
        private const float SectionPadding = 2f;
        private const float ToolbarGap = 2f;
        private const float ToolbarWidth = 20f;

        private List<T> filteredItems = new List<T>();
        private readonly Dictionary<T, string> labelCache = new Dictionary<T, string>();
        private readonly Dictionary<T, string> tipCache = new Dictionary<T, string>();
        private bool windowPinned;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchTerms = "";
    }
}
