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
        UpdateDomainTransition();
        if (LongEventHandler.AnyEventNowOrWaiting) return;
        ProcessDomainPreloads();
        UpdateDomains();
        ProcessDomainReturns();
        ProcessDomainOpenings();
    }

    public void OnGUI() => transition?.Draw();

    public void StartedNewGame() => InitializeDomainPreloads();

    public void LoadedGame()
    {
        InitializeDomainPreloads();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Current.Game != game) return;
            foreach (Map realm in domainMaps) realm.GetComponent<MapComponent_Domain>().EndArrivalTransition();
            foreach (Map loaded in game.Maps) loaded.GetComponent<MapComponent_DomainTransitionLock>().End();
        });
    }

    public void MapAdded(Map map) => preloadCheckRequested = true;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref domainMaps, "domainMaps", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) domainMaps ??= new();
    }

    public void Dispose()
    {
        SetTransition(null);
        foreach (Map realm in domainMaps) realm.GetComponent<MapComponent_Domain>().Worker?.Dispose();
        domainMaps.Clear();
        openings.Clear();
        returns.Clear();
        pendingDomainPreloads.Clear();
        preparing.Clear();
        failedPreparation.Clear();
        updatingDomains.Clear();
    }

    private readonly GameComponent_MapManager host;
}
