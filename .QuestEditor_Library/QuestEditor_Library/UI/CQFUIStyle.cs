using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFUIStyle
    {
        public static Color Canvas => new Color(0.075f, 0.085f, 0.10f);
        public static Color Panel => new Color(0.13f, 0.15f, 0.18f);
        public static Color Card => new Color(0.17f, 0.195f, 0.225f);
        public static Color Header => new Color(0.235f, 0.265f, 0.30f);
        public static Color Button => new Color(0.23f, 0.26f, 0.30f);
        public static Color Hover => new Color(0.29f, 0.325f, 0.365f);
        public static Color Border => new Color(0.38f, 0.42f, 0.47f);
        public static Color Accent => new Color(0.74f, 0.82f, 0.89f);
        public static Color Link => new Color(0.43f, 0.51f, 0.58f);
        public static Color TextColor => new Color(0.96f, 0.97f, 0.98f);
        public static Color Muted => new Color(0.76f, 0.79f, 0.83f);

        public static bool ButtonText(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, TextAnchor? overrideTextAnchor = null)
            => ButtonText(rect, label, drawBackground, doMouseoverSound, TextColor, active, overrideTextAnchor);

        public static bool ButtonText(Rect rect, string label, bool drawBackground, bool doMouseoverSound, Color textColor, bool active = true, TextAnchor? overrideTextAnchor = null)
        {
            using CQFUIScope scope = new CQFUIScope();
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && active;
            try
            {
                if (drawBackground) DrawBackground(rect);
                Text.Anchor = overrideTextAnchor ?? (drawBackground ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
                Text.WordWrap = rect.height > 40f;
                GUI.color *= GUI.enabled ? textColor : Muted;
                Rect text = rect.ContractedBy(Mathf.Min(6f, rect.width / 4f), 0f);
                Widgets.Label(text, Text.WordWrap ? label : (label ?? string.Empty).Truncate(text.width));
                return Widgets.ButtonInvisible(rect, doMouseoverSound);
            }
            finally { GUI.enabled = enabled; }
        }

        public static bool ButtonImage(Rect rect, Texture2D texture, bool doMouseoverSound = true, string? tooltip = null)
            => ButtonImage(rect, texture, TextColor, Accent, doMouseoverSound, tooltip);

        public static bool ButtonImage(Rect rect, Texture2D texture, Color baseColor, bool doMouseoverSound = true, string? tooltip = null)
            => ButtonImage(rect, texture, baseColor, Accent, doMouseoverSound, tooltip);

        public static bool ButtonImage(Rect rect, Texture2D texture, Color baseColor, Color mouseoverColor, bool doMouseoverSound = true, string? tooltip = null)
        {
            Color previous = GUI.color;
            try
            {
                GUI.color *= GUI.enabled ? Mouse.IsOver(rect) ? mouseoverColor : baseColor : Muted;
                DrawGlyph(rect, texture);
            }
            finally { GUI.color = previous; }
            if (tooltip != null) TooltipHandler.TipRegion(rect, tooltip);
            return Widgets.ButtonInvisible(rect, doMouseoverSound);
        }

        public static void DrawGlyph(Rect rect, Texture2D texture)
        {
            bool grayscale = texture == TexButton.Plus || texture == TexButton.Minus || texture == TexButton.ReorderDown || texture == TexButton.ReorderUp;
            Widgets.DrawTextureFitted(rect, texture, 1f, grayscale ? TexUI.GrayscaleGUI : null);
        }

        public static void DrawBackground(Rect rect, bool selected = false)
        {
            Widgets.DrawBoxSolid(rect, GUI.enabled && Mouse.IsOver(rect) ? Hover : selected ? Header : Button);
            DrawBox(rect);
        }

        public static void DrawBox(Rect rect, int thickness = 1, Texture2D? lineTexture = null)
        {
            Color previous = GUI.color;
            try { GUI.color = new Color(Border.r, Border.g, Border.b, previous.a); Widgets.DrawBox(rect, thickness); }
            finally { GUI.color = previous; }
        }

        public static void DrawMenuSection(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, Panel);
            DrawBox(rect);
        }

        public static void DrawHighlightSelected(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, Header);
            DrawBox(rect);
        }
    }
}
