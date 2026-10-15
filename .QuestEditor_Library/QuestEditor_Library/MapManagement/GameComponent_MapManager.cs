using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager : GameComponent
{
    public GameComponent_MapManager(Game game)
    {
        this.game = game;
        instance = this;
    }

    public static GameComponent_MapManager? Instance
    {
        get
        {
            Game currentGame = Current.Game;
            if (currentGame == null) return null;
            if (instance == null || !ReferenceEquals(instance.game, currentGame))
                instance = currentGame.GetComponent<GameComponent_MapManager>();
            return instance;
        }
    }

    public override void GameComponentUpdate()
    {
        if (saving) return;
        try
        {
            UpdateDomainTransition();
            if (LongEventHandler.AnyEventNowOrWaiting) return;
            ProcessDomainPreloads();
            UpdateDomains();
            ProcessDomainReturns();
            ProcessDomainOpenings();
        }
        finally
        {
            if (pendingHibernation.Count > 0) FlushRequests();
        }
    }

    public override void GameComponentOnGUI() => transition?.Draw();

    public override void StartedNewGame() => InitializeDomainPreloads();

    public override void LoadedGame()
    {
        RestoreHibernatingMaps();
        InitializeDomainPreloads();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Current.Game != game) return;
            foreach (Map realm in domainMaps) realm.GetComponent<MapComponent_Domain>().EndArrivalTransition();
            foreach (Map loaded in game.Maps) loaded.GetComponent<MapComponent_DomainTransitionLock>().End();
        });
    }

    public override void ExposeData()
    {
        ExposeHibernationData();
        Scribe_Collections.Look(ref domainMaps, "domainMaps", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) domainMaps ??= new();
    }

    public void DisposeMaps()
    {
        SetTransition(null);
        foreach (Map realm in domainMaps) realm.GetComponent<MapComponent_Domain>().Worker?.Dispose();
        domainMaps.Clear();
        DisposeHibernatingMaps();
        openings.Clear();
        returns.Clear();
        pendingDomainPreloads.Clear();
        preparing.Clear();
        failedPreparation.Clear();
        updatingDomains.Clear();
        if (ReferenceEquals(instance, this)) instance = null;
    }

    private readonly Game game;
    private static GameComponent_MapManager? instance;
}
