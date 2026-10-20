using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public static class DomainHealthBar
{
    public static Rect GetRect(ColonistBar bar, int row = 0)
    {
        _ = bar.Entries;
        float bottom = 58f;
        foreach (Vector2 location in bar.DrawLocs)
            bottom = Mathf.Max(bottom, location.y + bar.Size.y + 36f * bar.Scale);
        float width = Mathf.Min(520f, UI.screenWidth - 48f);
        return new Rect((UI.screenWidth - width) / 2f, bottom + 8f + row * 60f, width, 52f);
    }

    public static void Draw(MapComponent_Domain domain, string? title = null)
    {
        if (!domain.Active || domain.Caster == null || Find.CurrentMap != domain.map || !WorldRendererUtility.DrawingMap
            || Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;
        MapComponent_Domain battlefield = domain.Battlefield;
        int row = !ReferenceEquals(domain, battlefield) && battlefield.Active ? 1 : 0;
        Rect rect = GetRect(Find.ColonistBar, row);
        using CQFUIScope scope = new(rect.width, rect.height);
        GUI.color = Color.white;
        Widgets.DrawBoxSolid(rect, new Color(0.055f, 0.018f, 0.024f, 0.94f));
        GUI.color = new Color(0.42f, 0.16f, 0.17f);
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
        Text.WordWrap = false;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x + 12f, rect.y + 2f, rect.width * 0.62f, 24f),
            (title ?? domain.Definition.LabelCap) + " · " + domain.Caster.LabelShortCap);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(new Rect(rect.x + rect.width * 0.62f, rect.y + 2f, rect.width * 0.38f - 12f, 24f),
            domain.Health.ToString("N0") + " / " + domain.MaximumHealth.ToString("N0"));
        Rect track = new(rect.x + 12f, rect.y + 30f, rect.width - 24f, 12f);
        Widgets.DrawBoxSolid(track, new Color(0.015f, 0.005f, 0.009f));
        Rect fill = track.ContractedBy(1f);
        fill.width *= domain.HealthFraction;
        Widgets.DrawBoxSolid(fill, Color.Lerp(new Color(0.34f, 0.025f, 0.045f), new Color(0.72f, 0.035f, 0.075f), domain.HealthFraction));
        if (fill.width > 0f)
            Widgets.DrawBoxSolid(new Rect(fill.x, fill.y, fill.width, 2f), new Color(0.93f, 0.22f, 0.25f, 0.65f));
        for (int index = 1; index < 10; index++)
            Widgets.DrawBoxSolid(new Rect(track.x + track.width * index / 10f, track.y + 1f, 1f, track.height - 2f),
                new Color(0.08f, 0.015f, 0.022f, 0.75f));
    }
}
