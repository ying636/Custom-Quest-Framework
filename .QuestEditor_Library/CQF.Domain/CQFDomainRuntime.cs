using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime : ICQFMapModule
{
    public CQFDomainRuntime(GameComponent_MapManager host) { this.host = host; }

    public static CQFDomainRuntime Instance => GameComponent_MapManager.Instance?.Module as CQFDomainRuntime
        ?? throw new InvalidOperationException("[CQF] Domain module is not enabled for the current game.");

    private Game game => host.Game;

    public void Update()
    {
        if (!LongEventHandler.AnyEventNowOrWaiting) ProcessOpeningFusions();
        UpdateDomainTransition();
        if (LongEventHandler.AnyEventNowOrWaiting) return;
        ProcessDomainPreloads();
        UpdateDomains();
        ProcessDomainReturns();
        ProcessDomainOpenings();
        ProcessDomainContests();
    }

    public void OnGUI() => transition?.Draw();

    public void StartedNewGame() => InitializeDomainPreloads();

    public void LoadedGame()
    {
        contestCheckRequested = true;
        InitializeDomainPreloads();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Current.Game != game) return;
            foreach (Map realm in domainMaps)
            {
                realm.GetComponent<MapComponent_Domain>().EndArrivalTransition();
                realm.GetComponent<MapComponent_Domain>().Challenger?.EndArrivalTransition();
            }
            foreach (Map loaded in game.Maps) loaded.GetComponent<MapComponent_DomainTransitionLock>().End();
        });
    }

    public void MapAdded(Map map) => preloadCheckRequested = true;

    internal void RegisterVisuals(DomainOpeningVisuals visuals) => openingVisuals.Add(visuals);

    internal void UnregisterVisuals(DomainOpeningVisuals visuals) => openingVisuals.Remove(visuals);

    internal void RegisterSurface(DomainSurfaceVisuals surface) => surfaces.Add(surface);

    internal void UnregisterSurface(DomainSurfaceVisuals surface) => surfaces.Remove(surface);

    public void ExposeData()
    {
        Scribe_Collections.Look(ref domainMaps, "domainMaps", LookMode.Reference);
        Scribe_Collections.Look(ref mapTemplates, "mapTemplates", LookMode.Def, LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            domainMaps ??= new();
            mapTemplates ??= new();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        List<Exception> errors = new();
        try
        {
            List<Action> releases = new() { () => SetTransition(null) };
            if (domainMaps != null)
                foreach (Map? realm in domainMaps.ToArray())
                {
                    MapComponent_Domain? domain = realm?.GetComponent<MapComponent_Domain>();
                    if (domain == null) continue;
                    releases.Add(domain.ReleaseScreenBorder);
                    if (domain.Challenger is { } challenger) releases.Add(challenger.ReleaseScreenBorder);
                    if (domain.Worker is { } worker) releases.Add(worker.Dispose);
                    if (domain.Challenger?.Worker is { } challengerWorker) releases.Add(challengerWorker.Dispose);
                    if (domain.Contest is { } contest) releases.Add(contest.Dispose);
                }
            foreach (DomainOpeningVisuals visuals in openingVisuals.ToArray()) releases.Add(visuals.Dispose);
            foreach (DomainOpening_Fusion fusion in activeOpenings.OfType<DomainOpening_Fusion>()) releases.Add(fusion.ReleaseVisuals);
            foreach (DomainSurfaceVisuals surface in surfaces.ToArray()) releases.Add(surface.Dispose);
            foreach (Action release in releases)
            {
                try { release(); }
                catch (Exception error) { errors.Add(error); }
            }
        }
        finally
        {
            transition = null;
            openingVisuals.Clear();
            surfaces.Clear();
            mapTemplates?.Clear();
            domainMaps?.Clear();
            openings.Clear();
            activeOpenings.Clear();
            returns.Clear();
            processingReturns.Clear();
            pendingDomainPreloads.Clear();
            preparing.Clear();
            failedPreparation.Clear();
            updatingDomains.Clear();
            failedContestPairs.Clear();
        }
        if (errors.Count > 0) throw new AggregateException("[CQF] Domain resource disposal failed.", errors);
    }

    private readonly GameComponent_MapManager host;
    private readonly HashSet<DomainOpeningVisuals> openingVisuals = new();
    private readonly HashSet<DomainSurfaceVisuals> surfaces = new();
    private bool disposed;
}
