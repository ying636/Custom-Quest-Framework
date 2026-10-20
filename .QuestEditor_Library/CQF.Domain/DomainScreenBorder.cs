using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainScreenBorder : IDisposable
{
    public DomainScreenBorder(DomainSurfaceDef definition)
    {
        this.definition = definition;
        visuals = new DomainSurfaceVisuals(definition);
    }

    public void Draw(MapComponent_Domain domain)
    {
        if (!domain.Active || Find.CurrentMap != domain.map || !WorldRendererUtility.DrawingMap
            || Event.current.type != EventType.Repaint || Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;
        Vector3 low = UI.UIToMapPosition(0f, 0f);
        Vector3 high = UI.UIToMapPosition(UI.screenWidth, UI.screenHeight);
        Vector4 view = new(low.x, low.z, high.x - low.x, high.z - low.z);
        Vector4 boundary = default;
        DomainTerritoryPartition? territory = null;
        MapComponent_Domain battlefield = domain.Battlefield;
        if (battlefield.Active && battlefield.Challenger?.Active == true && domain.Contest is { } contest)
        {
            territory = contest.Partition;
            boundary = new Vector4(contest.Partition.BoundaryX,
                ReferenceEquals(domain, battlefield) == !contest.Partition.Reversed ? -1f : 1f, 0f, 0f);
        }
        visuals.SetTerritory(territory, ReferenceEquals(domain, battlefield));
        Color previous = GUI.color;
        try
        {
            GUI.color = Color.white;
            visuals.DrawScreen(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), view, boundary, default,
                new Vector4(UI.screenWidth / (float)Mathf.Max(1, UI.screenHeight), definition.screenBorderWidth, 0f, 2f),
                GenTicks.TicksGame / 60f, definition.screenBorderOpacity);
        }
        finally { GUI.color = previous; }
    }

    public void Dispose() => visuals.Dispose();

    private readonly DomainSurfaceDef definition;
    private readonly DomainSurfaceVisuals visuals;
}
