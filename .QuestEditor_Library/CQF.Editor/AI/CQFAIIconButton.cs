using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIIconButton
    {
        public static bool Draw(Rect rect, CQFAIIcon icon, string tip, bool filled = false)
        {
            TooltipHandler.TipRegion(rect, tip);
            bool enabled = GUI.enabled;
            bool hover = enabled && Mouse.IsOver(rect);
            Color color = enabled ? new Color(0.86f, 0.89f, 0.88f) : new Color(0.42f, 0.46f, 0.45f);
            if (filled)
            {
                DrawSurface(rect, enabled ? new Color(0.67f, 0.82f, 0.72f) : new Color(0.22f, 0.27f, 0.25f), 7);
                if (enabled) color = new Color(0.10f, 0.17f, 0.13f);
            }
            else if (hover) DrawSurface(rect, new Color(0.22f, 0.27f, 0.25f), 5);
            DrawIcon(rect.ContractedBy(4f), icon, color);
            return Widgets.ButtonInvisible(rect);
        }

        public static void DrawSurface(Rect rect, Color color, int radius)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + radius, rect.width, rect.height - radius * 2f), color);
            for (int row = 0; row < radius; row++)
            {
                float offset = radius - Mathf.Sqrt(radius * radius - Mathf.Pow(radius - row - 0.5f, 2f));
                Widgets.DrawBoxSolid(new Rect(rect.x + offset, rect.y + row, rect.width - offset * 2f, 1f), color);
                Widgets.DrawBoxSolid(new Rect(rect.x + offset, rect.yMax - row - 1f, rect.width - offset * 2f, 1f), color);
            }
        }

        private static void DrawIcon(Rect rect, CQFAIIcon icon, Color color)
        {
            Vector2 Point(float x, float y) => new Vector2(rect.x + x * rect.width / 24f, rect.y + y * rect.height / 24f);
            void Line(float x1, float y1, float x2, float y2) => Widgets.DrawLine(Point(x1, y1), Point(x2, y2), color, 0.55f);
            switch (icon)
            {
                case CQFAIIcon.Clear:
                    Line(4, 6, 20, 6); Line(8, 3, 16, 3); Line(8, 3, 8, 6); Line(16, 3, 16, 6);
                    Line(6, 6, 7, 21); Line(7, 21, 17, 21); Line(17, 21, 18, 6);
                    Line(10, 10, 10, 17); Line(14, 10, 14, 17);
                    break;
                case CQFAIIcon.Undo:
                    Line(9, 4, 4, 9); Line(4, 9, 9, 14); Line(4, 9, 14, 9);
                    Line(14, 9, 19, 12); Line(19, 12, 19, 17); Line(19, 17, 16, 20); Line(16, 20, 11, 20);
                    break;
                case CQFAIIcon.Settings:
                    for (int step = 0; step < 16; step++)
                    {
                        float angle = step * Mathf.PI / 8f;
                        float next = (step + 1) * Mathf.PI / 8f;
                        Line(12 + Mathf.Cos(angle) * 4f, 12 + Mathf.Sin(angle) * 4f, 12 + Mathf.Cos(next) * 4f, 12 + Mathf.Sin(next) * 4f);
                        float outer = step % 2 == 0 ? 10f : 7f;
                        float nextOuter = step % 2 == 0 ? 7f : 10f;
                        Line(12 + Mathf.Cos(angle) * outer, 12 + Mathf.Sin(angle) * outer, 12 + Mathf.Cos(next) * nextOuter, 12 + Mathf.Sin(next) * nextOuter);
                    }
                    break;
                case CQFAIIcon.Collapse:
                    Line(5, 12, 19, 12);
                    break;
                case CQFAIIcon.Expand:
                    Line(5, 12, 19, 12); Line(12, 5, 12, 19);
                    break;
                case CQFAIIcon.Send:
                    Line(12, 20, 12, 4); Line(5, 11, 12, 4); Line(12, 4, 19, 11);
                    break;
                case CQFAIIcon.Stop:
                    Widgets.DrawBoxSolid(new Rect(Point(6, 6), Point(18, 18) - Point(6, 6)), color);
                    break;
                case CQFAIIcon.Options:
                    Line(3, 6, 21, 6); Line(3, 12, 21, 12); Line(3, 18, 21, 18);
                    Line(8, 3, 8, 9); Line(16, 9, 16, 15); Line(8, 15, 8, 21);
                    break;
                case CQFAIIcon.Close:
                    Line(5, 5, 19, 19); Line(19, 5, 5, 19);
                    break;
            }
        }
    }
}
