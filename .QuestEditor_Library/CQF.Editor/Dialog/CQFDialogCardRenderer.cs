using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogCardRenderer
    {
        public float MeasureBody(string text, float scale)
        {
            GUIStyle style = this.Style(GameFont.Small, scale, false);
            return Mathf.Clamp(style.CalcHeight(new GUIContent(text.StripTags()), (CQFDialogNodeLayout.Width - 28f) * scale) / scale, 48f, 120f);
        }

        public void Frame(CQFDialogCardLayout layout, bool selected, string title)
        {
            Widgets.DrawBoxSolid(layout.Bounds, CQFEditorPalette.Card);
            Widgets.DrawBoxSolid(layout.Header, CQFEditorPalette.Header);
            Color previous = GUI.color;
            try { GUI.color = selected ? CQFEditorPalette.Accent : CQFEditorPalette.Border; Widgets.DrawBox(layout.Bounds, selected ? 2 : 1); }
            finally { GUI.color = previous; }
            this.Label(layout.Title, title, GameFont.Tiny, layout.Scale, true);
            this.Port(layout.Input, selected ? CQFEditorPalette.Accent : CQFEditorPalette.Link, layout.Scale);
        }

        public void Label(Rect rect, string text, GameFont font, float scale, bool singleLine = false)
        {
            GUIStyle style = this.Style(font, scale, singleLine);
            string value = singleLine ? text.Replace('\r', ' ').Replace('\n', ' ').StripTags() : text;
            if (singleLine && style.CalcSize(new GUIContent(value)).x > rect.width)
            {
                int low = 0, high = value.Length;
                while (low < high)
                {
                    int middle = (low + high + 1) / 2;
                    if (style.CalcSize(new GUIContent(value.Substring(0, middle) + "…")).x <= rect.width) low = middle;
                    else high = middle - 1;
                }
                if (low > 0 && char.IsHighSurrogate(value[low - 1])) low--;
                value = value.Substring(0, low) + "…";
            }
            GUI.BeginGroup(rect);
            try { GUI.Label(new Rect(0f, 0f, rect.width, rect.height), value, style); }
            finally { GUI.EndGroup(); }
        }

        public void Port(Vector2 point, Color color, float scale) => Widgets.DrawBoxSolid(new Rect(point.x - 4f * scale, point.y - 4f * scale, 8f * scale, 8f * scale), color);

        private GUIStyle Style(GameFont font, float scale, bool singleLine)
        {
            GameFont previous = Text.Font;
            Text.Font = font;
            try
            {
                GUIStyle original = Text.CurFontStyle;
                int fontSize = Mathf.Max(1, Mathf.RoundToInt((original.fontSize > 0 ? original.fontSize : original.font.fontSize) * scale));
                if (!this.styles.TryGetValue(font, out GUIStyle style) || style.fontSize != fontSize || style.font != original.font)
                {
                    style = new GUIStyle(original) { fontSize = fontSize, padding = new RectOffset(0, 0, 0, 0), contentOffset = Vector2.zero };
                    style.normal.textColor = CQFEditorPalette.Text;
                    this.styles[font] = style;
                }
                style.alignment = singleLine ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft;
                style.wordWrap = !singleLine;
                return style;
            }
            finally { Text.Font = previous; }
        }

        private readonly Dictionary<GameFont, GUIStyle> styles = new Dictionary<GameFont, GUIStyle>();
    }
}
