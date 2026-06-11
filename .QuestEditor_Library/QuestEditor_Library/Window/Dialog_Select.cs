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
        public Dialog_Select(List<T> ts, Func<T, Texture2D> getTexture, Func<T, string> getText, string title, Action<T> acceptAction, Func<T, Color> getColor = null,
            Action<T, Rect> drawAction = null, Func<T, string> getTip = null, Func<T, int> getPriority = null,
            List<ExtraOption> extraOptions = null, Func<T, string> getRawText = null, Dictionary<string, Func<T, bool>> typeFilters = null,
            Dictionary<string, string> typeTips = null)
        {
            this.ts = ts;
            this.getTexture = getTexture;
            this.getText = getText;
            this.acceptAction = acceptAction;
            this.title = title;
            this.getColor = getColor;
            this.drawAction = drawAction;
            this.getTip = getTip;
            this.getPriority = getPriority;
            this.extraOptions = extraOptions;
            this.getRawText = getRawText;
            this.typeFilters = typeFilters;
            this.typeTips = typeTips;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public void UpdateTs()
        {
            List<T> result = this.ts.Where(this.CanShowItem).ToList();
            if (this.getPriority != null)
            {
                result.SortBy(this.getPriority);
            }
            this.items = result;
        }

        protected override float Margin => DialogMargin;

        public override void DoWindowContents(Rect inRect)
        {
            if (!this.items.Any())
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
            contentHeight = this.DrawItems(this.items, viewRect, contentHeight);
            contentHeight = this.DrawExtraOptions(contentHeight, viewRect);
            Widgets.EndScrollView();
            this.height = contentHeight;
        }

        private float DrawTypeFilter(float y, float width, float selectWidth)
        {
            if (this.typeFilters == null)
            {
                return y;
            }

            string selectedTypeText = this.selectedTypeFilter ?? "CQF_DialogSelectAllTypes".Translate().ToString();
            Rect rect = new Rect(this.GetCenteredX(selectWidth, width), y, selectWidth, TypeFilterHeight);
            Text.Font = GameFont.Medium;
            if (Widgets.ButtonText(rect, "CQF_DialogSelectType".Translate(selectedTypeText), 
                    false,true,true,TextAnchor.MiddleCenter))
            {
                Find.WindowStack.Add(new FloatMenu(this.GetTypeFilterOptions()));
            }
            Text.Font = GameFont.Small;
            string tip = this.GetSelectedTypeTip();
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

        private float DrawItems(List<T> items, Rect viewRect, float contentHeight)
        {
            float x = this.getTexture != null ? this.GetFirstIconX(viewRect.width) : this.GetCenteredX(TextRowWidth, viewRect.width);
            float y = contentHeight;
            foreach (T t in items)
            {
                Rect rect = new Rect(x, y, this.getTexture != null ? IconSize : TextRowWidth, RowHeight);
                if (this.getTexture != null)
                {
                    this.DrawTextureItem(t, rect);
                    x += IconSpacing;
                    if (x + IconSize > viewRect.width)
                    {
                        x = this.GetFirstIconX(viewRect.width);
                        y += RowSpacing;
                    }
                }
                else
                {
                    y = this.DrawTextItem(t, rect, y);
                }
                TooltipHandler.TipRegion(rect, this.GetTip(t));
            }
            if (this.getTexture != null && x > 0f)
            {
                y += RowSpacing;
            }
            return y;
        }

        private float DrawExtraOptions(float y, Rect viewRect)
        {
            if (this.extraOptions == null)
            {
                return y;
            }

            foreach (var option in this.extraOptions)
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

        private float GetFirstIconX(float width)
        {
            return this.GetCenteredX(this.GetIconRowWidth(width), width);
        }

        private float GetIconRowWidth(float width)
        {
            int columnCount = Mathf.Max(1, Mathf.FloorToInt((width + IconSpacing - IconSize) / IconSpacing));
            return (columnCount - 1) * IconSpacing + IconSize;
        }

        private void DrawTextureItem(T t, Rect rect)
        {
            if (this.getColor != null && this.getColor(t) is Color color)
            {
                GUI.color = color;
            }

            if (this.drawAction != null)
            {
                this.drawAction(t, rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    this.AcceptAndClose(t);
                }
            }
            else if (this.getTexture(t) is Texture2D tex)
            {
                if (Widgets.ButtonImage(rect, tex, GUI.color))
                {
                    this.AcceptAndClose(t);
                }
            }
            else if (Widgets.ButtonInvisible(rect))
            {
                this.AcceptAndClose(t);
            }
            GUI.color = Color.white;
        }

        private float DrawTextItem(T t, Rect rect, float y)
        {
            string label = this.getText(t) ?? "";
            rect.height = Text.CalcHeight(label, rect.width);
            Color color = this.getColor == null || this.getColor(t) == null ? Widgets.NormalOptionColor : this.getColor(t);
            if (Widgets.ButtonText(rect, label, false, true, color))
            {
                this.AcceptAndClose(t);
            }
            return y + rect.height + 5f;
        }

        private void AcceptAndClose(T t)
        {
            this.acceptAction(t);
            this.Close();
        }

        private List<FloatMenuOption> GetTypeFilterOptions()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogSelectAllTypes".Translate(), delegate
                {
                    this.selectedTypeFilter = null;
                    this.UpdateTs();
                })
            };
            foreach (string filterLabel in this.typeFilters.Keys)
            {
                string label = filterLabel;
                options.Add(new FloatMenuOption(label, delegate
                {
                    this.selectedTypeFilter = label;
                    this.UpdateTs();
                }));
            }
            return options;
        }

        private string GetSelectedTypeTip()
        {
            if (this.typeTips == null || this.selectedTypeFilter == null)
            {
                return null;
            }
            return this.typeTips.TryGetValue(this.selectedTypeFilter, out string tip) ? tip : null;
        }

        private bool CanShowItem(T t)
        {
            return this.PassesTypeFilter(t) && this.MatchesSearch(t);
        }

        private bool PassesTypeFilter(T t)
        {
            if (this.typeFilters == null || this.selectedTypeFilter == null)
            {
                return true;
            }
            return this.typeFilters.TryGetValue(this.selectedTypeFilter, out Func<T, bool> filter) && filter != null && filter(t);
        }

        private bool MatchesSearch(T t)
        {
            if (this.terms == "")
            {
                return true;
            }

            string text = this.getText(t);
            if (!string.IsNullOrEmpty(text) && text.IndexOf(this.terms, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string raw = this.getRawText?.Invoke(t);
            return !string.IsNullOrEmpty(raw) && raw.IndexOf(this.terms, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetTip(T t)
        {
            StringBuilder tip = new StringBuilder();
            tip.AppendLine(this.getText(t));
            if (this.getTip != null)
            {
                tip.AppendLine(this.getTip(t));
            }
            return tip.ToString().Trim();
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
        public List<T> items = new List<T>();
        public List<T> ts;
        public Action<T> acceptAction;
        public Func<T, Texture2D> getTexture;
        public Func<T, Color> getColor;
        public Func<T, string> getText;
        public Func<T, string> getRawText;
        public Func<T, string> getTip;
        public Dictionary<string, Func<T, bool>> typeFilters;
        public Dictionary<string, string> typeTips;
        public string selectedTypeFilter;
        public List<ExtraOption> extraOptions = new List<ExtraOption>();
        public Action<T, Rect> drawAction;
        public Vector2 pos = Vector2.zero;
        public float height;

        private const float IconSize = 30f;
        private const float IconSpacing = 35f;
        private const float DialogMargin = 10f;
        private const float RowHeight = 25f;
        private const float RowSpacing = 30f;
        private const float SearchAndTypeWidth = 480f;
        private const float ScrollbarWidth = 16f;
        private const float TextRowWidth = 500f;
        private const float TypeFilterHeight = 35f;
        private const float TypeFilterSpacing = 40f;
        private const float TitleHeight = 35f;
        private Func<T, int> getPriority = null;
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
