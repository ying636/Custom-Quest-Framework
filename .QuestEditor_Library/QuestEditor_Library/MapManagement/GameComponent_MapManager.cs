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
            if (currentGame?.components == null) return null;
            if (instance == null || instance.disposed || !ReferenceEquals(instance.game, currentGame))
                instance = currentGame.GetComponent<GameComponent_MapManager>();
            return instance != null && !instance.disposed ? instance : null;
        }
    }

    public Game Game => game;
    public ICQFMapModule? Module { get; }

    public override void GameComponentUpdate()
    {
        if (disposed || Scribe.mode != LoadSaveMode.Inactive) return;
        try
        {
            Module?.Update();
#if RIMWORLD_1_7
            ProcessCompositeTransfers();
            crossMap.Update(this);
            UpdateCompositeMaps();
#endif
        }
        finally
        {
            if (pendingHibernation.Count > 0) FlushRequests();
        }
    }

    public override void GameComponentOnGUI()
    {
        Module?.OnGUI();
#if RIMWORLD_1_7
        DrawMapEntranceTitle();
#endif
    }

    public override void StartedNewGame() => Module?.StartedNewGame();

    public override void LoadedGame()
    {
        RestoreHibernatingMaps();
#if RIMWORLD_1_7
        RestoreCompositeMaps();
        crossMap.Restore();
#endif
        Module?.LoadedGame();
    }

    public override void ExposeData()
    {
        ExposeHibernationData();
#if RIMWORLD_1_7
        Scribe_Deep.Look(ref crossMap!, "CQF_crossMap");
        crossMap ??= new();
        Scribe_Collections.Look(ref compositeMaps, "CQF_compositeMaps", LookMode.Deep);
        compositeMaps ??= new();
        Scribe_Collections.Look(ref shownEntranceTitles, "CQF_shownEntranceTitles", LookMode.Value);
        shownEntranceTitles ??= new();
#endif
        if (Module != null && Scribe.EnterNode("cqfMapModule"))
        {
            try { Module.ExposeData(); }
            finally { Scribe.ExitNode(); }
        }
    }

    public void DisposeMaps()
    {
        if (disposed) return;
        List<Exception>? errors = null;
        try { CQFModuleLoader.BeforeMapsDisposed(game); }
        catch (Exception error) { (errors ??= new()).Add(error); }
        disposed = true;
        try { Module?.Dispose(); }
        catch (Exception error) { (errors ??= new()).Add(error); }
        finally
        {
            try { DisposeHibernatingMaps(); }
            catch (Exception error) { (errors ??= new()).Add(error); }
            finally
            {
                if (ReferenceEquals(instance, this)) instance = null;
            }
        }
        if (errors != null) throw new AggregateException("[CQF] Map manager disposal failed.", errors);
    }

    private readonly Game game;
    private bool disposed;
    private static GameComponent_MapManager? instance;
}
