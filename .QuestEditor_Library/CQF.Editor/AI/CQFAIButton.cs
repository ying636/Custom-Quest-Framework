using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIButton
    {
        public static bool Draw(Rect rect)
        {
            TooltipHandler.TipRegion(rect, "CQF_AI_OpenHint".Translate());
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            Color color = GUI.enabled ? new Color(0.68f, 0.85f, 0.73f) : Color.gray;
            float unit = Mathf.Min(rect.width, rect.height) / 32f;
            float x = rect.center.x - 12f * unit;
            float y = rect.center.y - 10f * unit;
            Widgets.DrawBoxSolid(new Rect(x + 5f * unit, y + 5f * unit, 20f * unit, 15f * unit), color * new Color(0.65f, 0.65f, 0.65f, 1f));
            Widgets.DrawBoxSolid(new Rect(x + 19f * unit, y + 18f * unit, 4f * unit, 5f * unit), color);
            Widgets.DrawBoxSolid(new Rect(x, y, 21f * unit, 15f * unit), color);
            Widgets.DrawBoxSolid(new Rect(x + 3f * unit, y + 12f * unit, 4f * unit, 6f * unit), color);
            Color dot = new Color(0.15f, 0.22f, 0.18f);
            for (int index = 0; index < 3; index++)
                Widgets.DrawBoxSolid(new Rect(x + (4f + index * 5f) * unit, y + 6f * unit, 3f * unit, 3f * unit), dot);
            return Widgets.ButtonInvisible(rect);
        }
    }
}
