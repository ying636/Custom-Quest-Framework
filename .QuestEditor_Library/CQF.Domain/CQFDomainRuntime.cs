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
                if (realm == null) continue;
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
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        domainMaps ??= new();
        DropLostDomainMaps();
    }

    // A map reference that does not resolve while loading means the map was not saved at all - older
    // releases could lose every hibernating map (see GameComponent_MapManager.MigrateLegacyHibernatingMaps).
    // Left in place the hole is dereferenced by every caller that walks the list and is then written
    // back into the next save, so it has to be dropped as soon as the references are resolved.
    private void DropLostDomainMaps()
    {
        int dropped = domainMaps.RemoveAll(map => map == null);
        if (dropped > 0)
            Log.Warning("[CQF] Dropped " + dropped + " domain map reference" + (dropped == 1 ? "" : "s")
                + " that could not be resolved while loading.");
    }

    public void Dispose()
    {
        SetTransition(null);
        foreach (Map realm in domainMaps)
        {
            if (realm == null) continue;
            MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
            domain.ReleaseScreenBorder();
            domain.Challenger?.ReleaseScreenBorder();
            domain.Worker?.Dispose();
            domain.Challenger?.Worker?.Dispose();
            domain.Contest?.Dispose();
        }
        foreach (DomainOpeningVisuals visuals in openingVisuals.ToArray()) visuals.Dispose();
        foreach (DomainOpening_Fusion fusion in activeOpenings.OfType<DomainOpening_Fusion>()) fusion.ReleaseVisuals();
        foreach (DomainSurfaceVisuals surface in surfaces.ToArray()) surface.Dispose();
        mapTemplates.Clear();
        domainMaps.Clear();
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

    private readonly GameComponent_MapManager host;
    private readonly HashSet<DomainOpeningVisuals> openingVisuals = new();
    private readonly HashSet<DomainSurfaceVisuals> surfaces = new();
}
