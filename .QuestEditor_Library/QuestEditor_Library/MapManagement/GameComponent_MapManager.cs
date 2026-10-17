using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager : GameComponent
{
    public GameComponent_MapManager(Game game)
    {
        this.game = game;
        instance = this;
        Module = CQFModuleLoader.CreateMapModule(this);
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

    public Game Game => game;
    public ICQFMapModule? Module { get; }

    public override void GameComponentUpdate()
    {
        if (Scribe.mode != LoadSaveMode.Inactive) return;
        try
        {
            Module?.Update();
        }
        finally
        {
            if (pendingHibernation.Count > 0) FlushRequests();
        }
    }

    public override void GameComponentOnGUI() => Module?.OnGUI();

    public override void StartedNewGame() => Module?.StartedNewGame();

    public override void LoadedGame()
    {
        RestoreHibernatingMaps();
        Module?.LoadedGame();
    }

    public override void ExposeData()
    {
        ExposeHibernationData();
        if (Module != null && Scribe.EnterNode("cqfMapModule"))
        {
            try { Module.ExposeData(); }
            finally { Scribe.ExitNode(); }
        }
    }

    public void DisposeMaps()
    {
        try { Module?.Dispose(); }
        finally
        {
            DisposeHibernatingMaps();
            if (ReferenceEquals(instance, this)) instance = null;
        }
    }

    private readonly Game game;
    private static GameComponent_MapManager? instance;
}
