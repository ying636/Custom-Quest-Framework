using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIChatBubble
    {
        public static void Draw(Rect rect, Color color)
        {
            float radius = Mathf.Floor(Mathf.Min(10f, Mathf.Min(rect.width, rect.height) / 2f));
            if (radius < 1f)
            {
                Widgets.DrawBoxSolid(rect, color);
                return;
            }
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + radius, rect.width, rect.height - 2f * radius), color);
            for (int row = 0; row < radius; row++)
            {
                float distance = radius - row - 0.5f;
                float inset = radius - Mathf.Sqrt(radius * radius - distance * distance);
                Widgets.DrawBoxSolid(new Rect(rect.x + inset, rect.y + row, rect.width - 2f * inset, 1f), color);
                Widgets.DrawBoxSolid(new Rect(rect.x + inset, rect.yMax - row - 1f, rect.width - 2f * inset, 1f), color);
            }
        }
    }
}
