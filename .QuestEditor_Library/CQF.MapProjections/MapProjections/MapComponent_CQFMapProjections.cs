#if RIMWORLD_1_7
using Verse;

namespace QuestEditor_Library;

public sealed class MapComponent_CQFMapProjections : MapComponent, IDisposable
{
    public MapComponent_CQFMapProjections(Map map) : base(map) { }

    public IReadOnlyList<CQFMapProjection> Projections => projections;

    public CQFMapProjection Add(CQFMapProjection projection)
    {
        projection.Validate(map);
        if (projections.Any(other => other.id == projection.id))
            throw new InvalidOperationException("[CQF] Duplicate map projection ID: " + projection.id);
        if (projection.enabled) GameComponent_MapManager.Instance!.Activate(projection.sourceMap!);
        projections.Add(projection);
        return projection;
    }

    public bool Remove(string id)
    {
        CQFMapProjection? projection = projections.Find(candidate => candidate.id == id);
        if (projection == null) return false;
        projection.Dispose();
        return projections.Remove(projection);
    }

    public void Set(CQFMapProjection projection)
    {
        projection.Validate(map);
        if (projection.enabled) GameComponent_MapManager.Instance!.Activate(projection.sourceMap!);
        int index = projections.FindIndex(candidate => candidate.id == projection.id);
        if (index < 0) projections.Add(projection);
        else
        {
            projections[index].Dispose();
            projections[index] = projection;
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref projections, "CQF_mapProjections", LookMode.Deep);
        projections ??= new();
    }

    public override void MapComponentUpdate()
    {
        if (disposed || Scribe.mode != LoadSaveMode.Inactive || LongEventHandler.AnyEventNowOrWaiting) return;
        foreach (CQFMapProjection projection in projections)
        {
            if (!projection.enabled) { projection.Dispose(); continue; }
            try
            {
                projection.Validate(map);
                GameComponent_MapManager.Instance!.Activate(projection.sourceMap!);
            }
            catch (Exception error) { Fail(projection, error); }
        }
    }

    public override void MapComponentDraw()
    {
        if (disposed || CQFMapProjectionContext.IsRendering) return;
        foreach (CQFMapProjection projection in projections)
        {
            if (GameComponent_CQFProjectionUI.Instance?.Session?.Original == projection) continue;
            try { projection.Draw(map); }
            catch (Exception error) { Fail(projection, error); }
        }
        GameComponent_CQFProjectionUI.Instance?.DrawSession(map);
    }

    public override void MapRemoved() => Dispose();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (CQFMapProjection projection in projections) projection.Dispose();
        CQFMapProjectionContext.ReleaseHost(map);
        GameComponent_CQFProjectionUI.Instance?.NotifyMapRemoved(map);
    }

    public static bool KeepsActive(Map source) => KeepsActive(Current.Game, source);

    public static bool KeepsActive(Game? game, Map source)
    {
        if (game == null) return false;
        if (game.components != null && game.GetComponent<GameComponent_CQFProjectionUI>()?.IsSourceHeld(source) == true) return true;
        foreach (Map host in game.Maps)
        {
            MapComponent_CQFMapProjections? component = host.GetComponent<MapComponent_CQFMapProjections>();
            if (component is { disposed: false } && component.projections.Any(p => p.enabled && p.sourceMap == source)) return true;
        }
        return false;
    }

    private void Fail(CQFMapProjection projection, Exception error)
    {
        projection.enabled = false;
        projection.Dispose();
        Log.Error("[CQF] Disabled map projection " + projection.id + " on " + map.GetUniqueLoadID() + ": " + error);
    }

    private List<CQFMapProjection> projections = new();
    private bool disposed;
}
#endif
