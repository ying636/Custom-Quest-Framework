using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_Select<T> : Window
    {
        public Dialog_Select(SelectDrawer<T> drawer, string title)
        {
            this.drawer = drawer;
            this.title = title;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        protected override float Margin => DialogMargin;

        public void UpdateTs()
        {
            string selectedType = this.selectedType?.type;
            this.drawer.UpdateTypes(this.MatchesSearch);
            this.selectedType = selectedType == null ? null : this.drawer.types.FirstOrDefault(type => type.type == selectedType);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (this.drawer.ts == null)
            {
                this.UpdateTs();
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, TitleHeight), this.title);
            Text.Font = GameFont.Small;

            float y = this.Margin + 17f;
            float selectWidth = SearchAndTypeWidth;
            y = this.DrawTypeFilter(y, inRect.width, selectWidth);
            y = this.DrawSearchField(y, inRect.width, selectWidth);

            Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y);
            Rect viewRect = new Rect(0f, 0f, inRect.width - ScrollbarWidth, Mathf.Max(this.height, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float contentHeight = 0f;
            contentHeight = this.DrawItems(viewRect, contentHeight);
            contentHeight = this.DrawExtraOptions(contentHeight, viewRect);
            Widgets.EndScrollView();
            this.height = contentHeight;
        }

        private float DrawTypeFilter(float y, float width, float selectWidth)
        {
            if (this.drawer.types.NullOrEmpty())
            {
                return y;
            }

            string selectedTypeText = this.selectedType?.type ?? "CQF_DialogSelectAllTypes".Translate().ToString();
            Rect rect = new Rect(this.GetCenteredX(selectWidth, width), y, selectWidth, TypeFilterHeight);
            Text.Font = GameFont.Medium;
            if (Widgets.ButtonText(rect, "CQF_DialogSelectType".Translate(selectedTypeText), 
                    false,true,true,TextAnchor.MiddleCenter))
            {
                Find.WindowStack.Add(new FloatMenu(this.GetTypeFilterOptions()));
            }
            Text.Font = GameFont.Small;
            string tip = this.selectedType?.tip;
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tip);
            }
            return y + TypeFilterSpacing;
        }

        private float DrawSearchField(float y, float width, float selectWidth)
        {
            string text = Widgets.TextField(new Rect(this.GetCenteredX(selectWidth, width), y, selectWidth, RowHeight), this.terms);
            if (text != this.terms)
            {
                this.terms = text;
                this.UpdateTs();
            }
            return y + RowSpacing;
        }

        private float DrawItems(Rect viewRect, float contentHeight)
        {
            return this.drawer.DrawItems(viewRect, contentHeight, () => this.Close(), this.selectedType?.ts ?? this.drawer.ts);
        }

        private float DrawExtraOptions(float y, Rect viewRect)
        {
            if (this.drawer.extraOptions == null)
            {
                return y;
            }

            foreach (var option in this.drawer.extraOptions)
            {
                Rect rect = new Rect(this.GetCenteredX(TextRowWidth, viewRect.width), y, TextRowWidth, RowHeight);
                if (Widgets.ButtonText(rect, option.text, false, true, option.color))
                {
                    option.action();
                    this.Close();
                }
                y += RowSpacing;
                TooltipHandler.TipRegion(rect, this.GetTip(option));
            }
            return y;
        }

        private float GetCenteredX(float width, float parentWidth)
        {
            return Mathf.Max(0f, (parentWidth - width) / 2f);
        }

        private List<FloatMenuOption> GetTypeFilterOptions()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogSelectAllTypes".Translate(), delegate
                {
                    this.selectedType = null;
                    this.UpdateTs();
                })
            };
            foreach (SelectType<T> type in this.drawer.types)
            {
                SelectType<T> capturedType = type;
                options.Add(new FloatMenuOption(capturedType.type, delegate
                {
                    this.selectedType = capturedType;
                    this.UpdateTs();
                }));
            }
            return options;
        }

        private bool MatchesSearch(T t)
        {
            if (this.terms == "")
            {
                return true;
            }

            SelectItem<T> item = this.drawer.ItemFor(t);
            string text = item.text;
            if (!string.IsNullOrEmpty(text) && text.IndexOf(this.terms, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string raw = item.rawText;
            return !string.IsNullOrEmpty(raw) && raw.IndexOf(this.terms, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetTip(ExtraOption option)
        {
            StringBuilder tip = new StringBuilder();
            tip.AppendLine(option.text);
            if (option.tip != null)
            {
                tip.AppendLine(option.tip);
            }
            return tip.ToString().Trim();
        }

        public string title;
        public string terms = "";
        public SelectDrawer<T> drawer;
        public SelectType<T> selectedType;
        public Vector2 pos = Vector2.zero;
        public float height;

        private const float DialogMargin = 10f;
        private const float RowHeight = 25f;
        private const float RowSpacing = 30f;
        private const float SearchAndTypeWidth = 480f;
        private const float ScrollbarWidth = 16f;
        private const float TextRowWidth = 500f;
        private const float TypeFilterHeight = 35f;
        private const float TypeFilterSpacing = 40f;
        private const float TitleHeight = 35f;
    }

    public abstract class SelectDrawer<T>
    {
        protected SelectDrawer(List<T> ts, Func<T, string> getText, Action<T> acceptAction, Func<T, Color> getColor = null,
            Func<T, string> getTip = null, Func<T, int> getPriority = null, Func<T, string> getRawText = null,
            List<ExtraOption> extraOptions = null, Dictionary<string, Func<T, bool>> typeFilters = null,
            Dictionary<string, string> typeTips = null)
        {
            this.sourceTs = ts?.Where(t => t is not null).ToList() ?? new List<T>();
            this.getText = getText;
            this.acceptAction = acceptAction;
            this.getColor = getColor;
            this.getTip = getTip;
            this.getPriority = getPriority;
            this.getRawText = getRawText;
            this.extraOptions = extraOptions;
            this.rawTypeFilters = typeFilters;
            this.typeTips = typeTips;
        }

        public abstract float DrawItems(Rect viewRect, float contentHeight, Action closeAction, List<T> ts);

        public void UpdateTypes(Func<T, bool> canShowItem)
        {
            this.ts = this.FilterAndSort(this.sourceTs, canShowItem);
            this.types.Clear();
            if (this.rawTypeFilters == null)
            {
                return;
            }
            foreach (KeyValuePair<string, Func<T, bool>> filter in this.rawTypeFilters)
            {
                string type = filter.Key;
                Func<T, bool> canUse = filter.Value;
                this.types.Add(new SelectType<T>
                {
                    type = type,
                    tip = this.typeTips != null && this.typeTips.TryGetValue(type, out string tip) ? tip : null,
                    ts = canUse == null ? new List<T>() : this.FilterAndSort(this.sourceTs.Where(canUse), canShowItem)
                });
            }
        }

        public string GetText(T t)
        {
            return this.ItemFor(t).text;
        }

        public string GetRawText(T t)
        {
            return this.ItemFor(t).rawText;
        }

        public void Sort(List<T> ts)
        {
            if (this.getPriority != null)
            {
                ts.SortBy(t => this.ItemFor(t).priority);
            }
        }

        public SelectItem<T> ItemFor(T t)
        {
            if (!this.cachedItems.TryGetValue(t, out SelectItem<T> item))
            {
                item = this.CacheItem(t);
                this.cachedItems[t] = item;
            }
            return item;
        }

        protected virtual SelectItem<T> CacheItem(T t)
        {
            string text = this.getText?.Invoke(t) ?? "";
            string extraTip = this.getTip?.Invoke(t);
            StringBuilder tip = new StringBuilder();
            tip.AppendLine(text);
            if (extraTip != null)
            {
                tip.AppendLine(extraTip);
            }
            return new SelectItem<T>
            {
                value = t,
                text = text,
                rawText = this.getRawText?.Invoke(t),
                tip = tip.ToString().Trim(),
                color = this.getColor?.Invoke(t),
                priority = this.getPriority?.Invoke(t) ?? 0
            };
        }

        protected void BuildCache()
        {
            this.cachedItems.Clear();
            foreach (T t in this.sourceTs)
            {
                this.cachedItems[t] = this.CacheItem(t);
            }
            this.UpdateTypes(t => true);
        }

        protected float GetCenteredX(float width, float parentWidth)
        {
            return Mathf.Max(0f, (parentWidth - width) / 2f);
        }

        protected void AcceptAndClose(T t, Action closeAction)
        {
            this.acceptAction(t);
            closeAction();
        }

        private List<T> FilterAndSort(IEnumerable<T> source, Func<T, bool> canShowItem)
        {
            List<T> result = source.Where(canShowItem).ToList();
            this.Sort(result);
            return result;
        }

        private readonly Func<T, string> getText;
        private readonly Action<T> acceptAction;
        private readonly Func<T, Color> getColor;
        private readonly Func<T, string> getTip;
        private readonly Func<T, int> getPriority;
        private readonly Func<T, string> getRawText;
        private readonly Dictionary<string, Func<T, bool>> rawTypeFilters;
        private readonly Dictionary<string, string> typeTips;
        private readonly List<T> sourceTs;

        public List<ExtraOption> extraOptions;
        public List<SelectType<T>> types = new List<SelectType<T>>();
        public List<T> ts;
        private readonly Dictionary<T, SelectItem<T>> cachedItems = new Dictionary<T, SelectItem<T>>();
    }

    public class SelectType<T>
    {
        public string type;
        public string tip;
        public List<T> ts;
    }

    public class SelectItem<T>
    {
        public T value;
        public string text;
        public string rawText;
        public string tip;
        public Color? color;
        public int priority;
        public Texture2D texture;
    }

    public class TextSelectDrawer<T> : SelectDrawer<T>
    {
        public TextSelectDrawer(List<T> ts, Func<T, string> getText, Action<T> acceptAction, Func<T, Color> getColor = null,
            Func<T, string> getTip = null, Func<T, int> getPriority = null, Func<T, string> getRawText = null,
            List<ExtraOption> extraOptions = null, Dictionary<string, Func<T, bool>> typeFilters = null,
            Dictionary<string, string> typeTips = null)
            : base(ts, getText, acceptAction, getColor, getTip, getPriority, getRawText, extraOptions, typeFilters, typeTips)
        {
            this.BuildCache();
        }

        public override float DrawItems(Rect viewRect, float contentHeight, Action closeAction, List<T> ts)
        {
            float y = contentHeight;
            foreach (T t in ts)
            {
                SelectItem<T> item = this.ItemFor(t);
                Rect rect = new Rect(this.GetCenteredX(TextRowWidth, viewRect.width), y, TextRowWidth, RowHeight);
                string label = item.text;
                rect.height = Text.CalcHeight(label, rect.width);
                Color color = item.color ?? Widgets.NormalOptionColor;
                if (Widgets.ButtonText(rect, label, false, true, color))
                {
                    this.AcceptAndClose(item.value, closeAction);
                }
                TooltipHandler.TipRegion(rect, item.tip);
                y += rect.height + TextRowSpacing;
            }
            return y;
        }

        private const float RowHeight = 25f;
        private const float TextRowSpacing = 5f;
        private const float TextRowWidth = 500f;
    }

    public class TextureSelectDrawer<T> : SelectDrawer<T>
    {
        public TextureSelectDrawer(List<T> ts, Func<T, Texture2D> getTexture, Func<T, string> getText, Action<T> acceptAction,
            Func<T, Color> getColor = null, Action<T, Rect> drawAction = null, Func<T, string> getTip = null,
            Func<T, int> getPriority = null, Func<T, string> getRawText = null, List<ExtraOption> extraOptions = null,
            Dictionary<string, Func<T, bool>> typeFilters = null, Dictionary<string, string> typeTips = null)
            : base(ts, getText, acceptAction, getColor, getTip, getPriority, getRawText, extraOptions, typeFilters, typeTips)
        {
            this.getTexture = getTexture;
            this.drawAction = drawAction;
            this.BuildCache();
        }

        protected virtual float ItemHeight => IconSize;

        protected virtual float ItemSpacingX => IconSpacing;

        protected virtual float ItemWidth => IconSize;

        protected virtual float RowSpacing => IconRowSpacing;

        public override float DrawItems(Rect viewRect, float contentHeight, Action closeAction, List<T> ts)
        {
            float x = this.GetFirstX(viewRect.width);
            float y = contentHeight;
            foreach (T t in ts)
            {
                SelectItem<T> item = this.ItemFor(t);
                Rect rect = new Rect(x, y, this.ItemWidth, this.ItemHeight);
                this.DrawItem(item, rect, closeAction);
                TooltipHandler.TipRegion(rect, item.tip);
                x += this.ItemSpacingX;
                if (x + this.ItemWidth > viewRect.width)
                {
                    x = this.GetFirstX(viewRect.width);
                    y += this.RowSpacing;
                }
            }
            if (x > 0f)
            {
                y += this.RowSpacing;
            }
            return y;
        }

        protected virtual Rect GetImageRect(Rect rect)
        {
            return rect;
        }

        protected override SelectItem<T> CacheItem(T t)
        {
            SelectItem<T> item = base.CacheItem(t);
            item.texture = this.getTexture?.Invoke(t);
            return item;
        }

        protected virtual float DrawItem(SelectItem<T> item, Rect rect, Action closeAction)
        {
            Color color = item.color ?? Color.white;
            Color oldColor = GUI.color;
            GUI.color = color;
            Rect imageRect = this.GetImageRect(rect);
            if (this.drawAction != null)
            {
                this.drawAction(item.value, imageRect);
            }
            else if (item.texture is Texture2D tex)
            {
                Widgets.DrawTextureFitted(imageRect, tex, 1f);
            }
            GUI.color = oldColor;
            if (Widgets.ButtonInvisible(rect))
            {
                this.AcceptAndClose(item.value, closeAction);
            }
            return rect.height;
        }

        protected float GetFirstX(float width)
        {
            return this.GetCenteredX(this.GetRowWidth(width), width);
        }

        private float GetRowWidth(float width)
        {
            int columnCount = Mathf.Max(1, Mathf.FloorToInt((width + this.ItemSpacingX - this.ItemWidth) / this.ItemSpacingX));
            return (columnCount - 1) * this.ItemSpacingX + this.ItemWidth;
        }

        private readonly Func<T, Texture2D> getTexture;
        private readonly Action<T, Rect> drawAction;

        private const float IconSize = 30f;
        private const float IconSpacing = 35f;
        private const float IconRowSpacing = 30f;
    }

    public class LabeledTextureSelectDrawer<T> : TextureSelectDrawer<T>
    {
        public LabeledTextureSelectDrawer(List<T> ts, Func<T, Texture2D> getTexture, Func<T, string> getText, Action<T> acceptAction,
            Func<T, Color> getColor = null, Action<T, Rect> drawAction = null, Func<T, string> getTip = null,
            Func<T, int> getPriority = null, Func<T, string> getRawText = null, List<ExtraOption> extraOptions = null,
            Dictionary<string, Func<T, bool>> typeFilters = null, Dictionary<string, string> typeTips = null)
            : base(ts, getTexture, getText, acceptAction, getColor, drawAction, getTip, getPriority, getRawText, extraOptions, typeFilters, typeTips)
        {
        }

        protected override float ItemHeight => LabeledItemHeight;

        protected override float ItemSpacingX => LabeledItemSpacingX;

        protected override float ItemWidth => LabeledItemWidth;

        protected override float RowSpacing => LabeledRowSpacing;

        public override float DrawItems(Rect viewRect, float contentHeight, Action closeAction, List<T> ts)
        {
            float x = this.GetFirstX(viewRect.width);
            float y = contentHeight;
            float rowHeight = 0f;
            foreach (T t in ts)
            {
                SelectItem<T> item = this.ItemFor(t);
                Rect rect = new Rect(x, y, ItemWidth, 0f);
                float itemHeight = this.DrawItem(item, rect, closeAction);
                TooltipHandler.TipRegion(rect, item.tip);
                rowHeight = Mathf.Max(rowHeight, itemHeight);
                x += ItemSpacingX;
                if (x + ItemWidth > viewRect.width)
                {
                    x = this.GetFirstX(viewRect.width);
                    y += rowHeight + RowSpacing;
                    rowHeight = 0f;
                }
            }
            return y + rowHeight + RowSpacing;
        }

        protected override Rect GetImageRect(Rect rect)
        {
            return new Rect(rect.x + (rect.width - IconSize) / 2f, rect.y, IconSize, IconSize);
        }

        protected override float DrawItem(SelectItem<T> item, Rect rect, Action closeAction)
        {
            float labelHeight = Mathf.Max(1f, Text.CalcHeight(item.text, ItemWidth));
            float itemHeight = IconSize + LabelGap + labelHeight;
            rect.height = itemHeight;
            base.DrawItem(item, rect, closeAction);
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y + IconSize + LabelGap, rect.width, rect.height - IconSize - LabelGap), item.text);
            Text.Anchor = oldAnchor;
            return itemHeight;
        }

        private const float IconSize = 30f;
        private const float LabelGap = 3f;
        private const float LabeledItemHeight = 68f;
        private const float LabeledItemSpacingX = 90f;
        private const float LabeledItemWidth = 80f;
        private const float LabeledRowSpacing = 20f;
    }

    public class ExtraOption 
    {
        public ExtraOption(string text, string tip , Action action, Texture2D icon = null
            ,Color? color = null)
        {
            this.text = text;
            this.tip = tip;
            if (icon != null)
            {
                this.color = color.Value;
            }
            this.icon = icon;
            this.action = action;
        }
        public string text;
        public string tip;
        public Color color = Color.white;
        public Texture2D icon;
        public Action action;
    }
}
