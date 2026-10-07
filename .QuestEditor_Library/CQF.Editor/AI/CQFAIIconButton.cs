using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIIconButton
    {
        public static bool Draw(Rect rect, CQFAIIcon icon, string tip, bool accent = false)
        {
            TooltipHandler.TipRegion(rect, tip);
            Color color = GUI.enabled ? accent || Mouse.IsOver(rect) ? CQFEditorPalette.Accent : CQFEditorPalette.Text : CQFEditorPalette.Muted;
            DrawIcon(rect.ContractedBy(6f), icon, color);
            return Widgets.ButtonInvisible(rect);
        }

        public static bool DrawText(Rect rect, string label, bool selected = false)
        {
            TooltipHandler.TipRegion(rect, label);
            DrawBackground(rect, selected);
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Color previousColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                GUI.color = previousColor * (GUI.enabled ? CQFEditorPalette.Text : CQFEditorPalette.Muted);
                Widgets.Label(rect.ContractedBy(8f, 0f), label.Truncate(rect.width - 16f));
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; GUI.color = previousColor; }
            return Widgets.ButtonInvisible(rect);
        }

        public static bool DrawImage(Rect rect, Texture2D texture, string tip, bool framed = false)
        {
            TooltipHandler.TipRegion(rect, tip);
            if (framed) DrawBackground(rect);
            Color previous = GUI.color;
            try
            {
                GUI.color = previous * (GUI.enabled ? Mouse.IsOver(rect) ? CQFEditorPalette.Accent : CQFEditorPalette.Text : CQFEditorPalette.Muted);
                Widgets.DrawTextureFitted(rect.ContractedBy(7f), texture, 1f);
            }
            finally { GUI.color = previous; }
            return Widgets.ButtonInvisible(rect);
        }

        public static bool DrawFramedImage(Rect rect, Texture2D texture, string tip)
        {
            return DrawImage(rect, texture, tip, true);
        }

        public static void DrawBackground(Rect rect, bool selected = false)
        {
            Widgets.DrawBoxSolid(rect, GUI.enabled && Mouse.IsOver(rect) ? CQFEditorPalette.Hover : selected ? CQFEditorPalette.Header : CQFEditorPalette.Button);
            Color previous = GUI.color;
            try { GUI.color = selected ? CQFEditorPalette.Accent : CQFEditorPalette.Border; Widgets.DrawBox(rect); }
            finally { GUI.color = previous; }
        }

        public static void DrawSurface(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
        }

        private static void DrawIcon(Rect rect, CQFAIIcon icon, Color color)
        {
            Texture2D texture = icon switch
            {
                CQFAIIcon.NewChat => TexButton.NewFile,
                CQFAIIcon.History => ContentFinder<Texture2D>.Get("UI/QuestEditor/LoadQuest"),
                CQFAIIcon.Clear => TexButton.Delete,
                CQFAIIcon.Undo => TexButton.Delete,
                CQFAIIcon.Settings or CQFAIIcon.Options => ContentFinder<Texture2D>.Get("UI/Icon_Edit"),
                CQFAIIcon.Collapse => TexButton.Minus,
                CQFAIIcon.Expand => TexButton.Plus,
                CQFAIIcon.Send => ContentFinder<Texture2D>.Get("UI/Icon_Arrow"),
                CQFAIIcon.Stop => TexButton.Stop,
                CQFAIIcon.Close => TexButton.CloseXSmall,
                CQFAIIcon.More => TexButton.ReorderDown,
                _ => throw new ArgumentOutOfRangeException(nameof(icon))
            };
            Color previous = GUI.color;
            try
            {
                GUI.color = previous * color;
                Widgets.DrawTextureFitted(rect, texture, 1f);
            }
            finally { GUI.color = previous; }
        }
    }
}
