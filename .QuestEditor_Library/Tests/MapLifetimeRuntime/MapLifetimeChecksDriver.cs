using System.Reflection;
using System.Text;
using System.Text.Json;
using LudeonTK;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace CQF.MapLifetimeChecks;

public sealed class MapLifetimeChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 300f) throw new TimeoutException("CQF map lifetime runtime checks timed out.");
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            if (stage is 0 or 2 or 4)
            {
                if (Current.Game != null || !GenScene.InEntryScene) return;
                DebugActionNode menu = DebugActions_MapHibernation.MapHibernation();
                Check(menu.childGetter().Count == 0 && !menu.VisibleNow, "entry scene safely enumerates map hibernation after stage " + stage);
                Dialog_Debug.ResetStaticData();
                Dialog_Debug.TrySetupNodeGraph();
                Check(new Dialog_Debug(DefDatabase<DebugTabMenuDef>.GetNamed("Settings")).CurrentNode != null,
                    "native Settings debug tab initializes after stage " + stage);
                if (stage == 2)
                {
                    staleCommand!.action();
                    Check(!staleCommand.VisibleNow && !exitingManager!.IsHibernateRequested(exitingMap),
                        "cached command cannot mutate the game after returning to the main menu");
                }
                if (stage == 4)
                {
                    Check(GameComponent_MapManager.Instance == null, "completed scene change exposes no disposed map manager");
                    Finish(true);
                    return;
                }
                typeof(QuickStarter).GetField("quickStarted", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, true);
                stage++;
                SceneManager.LoadScene("Play");
                return;
            }
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            Current.Game.tickManager.CurTimeSpeed = TimeSpeed.Paused;
            GameComponent_MapManager host = GameComponent_MapManager.Instance ?? throw new InvalidOperationException("CQF map manager is missing.");
            CQFDomainRuntime runtime = host.Module as CQFDomainRuntime ?? throw new InvalidOperationException("CQF domain module is missing.");
            Map home = Find.CurrentMap;
            if (stage == 1)
            {
                DebugActionNode menu = DebugActions_MapHibernation.MapHibernation();
                staleCommand = menu.childGetter().First(node => node.label.EndsWith(home.GetUniqueLoadID(), StringComparison.Ordinal));
                Check(menu.VisibleNow && staleCommand.VisibleNow, "playing scene exposes current map commands");
                Map realm = runtime.Acquire(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"), home.mapPawns.FreeColonistsSpawned.First());
                runtime.Recycle(realm.GetComponent<MapComponent_Domain>(), home);
                host.GameComponentUpdate();
                Check(host.IsHibernating(realm) && !Current.Game.Maps.Contains(realm), "real domain map is detached and retained for exit cleanup");
                exitingManager = host;
                exitingMap = home;
                GenScene.GoToMainMenu();
                Check(realm.Disposed && home.Disposed && GameComponent_MapManager.Instance == null,
                    "native quit disposes both active and hibernating maps");
                stage = 2;
                return;
            }
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "CQF-PartialDomainState.xml");
            File.WriteAllText(path, "<save><runtime /></save>", Encoding.UTF8);
            Scribe.loader.InitLoading(path);
            if (!Scribe.EnterNode("runtime")) throw new InvalidOperationException("Runtime fixture node is missing.");
            try { runtime.ExposeData(); }
            finally { Scribe.ExitNode(); Scribe.ForceStop(); }
            Check(typeof(CQFDomainRuntime).GetField("domainMaps", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime) == null,
                "native interrupted load reproduces the reported null domain list");
            GenScene.GoToMainMenu();
            Check(home.Disposed && GameComponent_MapManager.Instance == null, "interrupted domain loading does not block native quit");
            stage = 4;
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MapLifetimeChecks.txt"), "FAIL " + error + "\n", Encoding.UTF8);
            Log.Error("[CQF map lifetime checks] " + error);
            Finish(false);
        }
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
        Log.Message("[CQF map lifetime checks] PASS " + message);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MapLifetimeChecks.txt"), "PASS " + message + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        finished = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MapLifetimeEvidence.json"), JsonSerializer.Serialize(new
        {
            success, checks, gameVersion = VersionControl.CurrentVersionString,
            core = typeof(GameComponent_MapManager).Assembly.Location, domain = typeof(CQFDomainRuntime).Assembly.Location
        }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        Log.Message("[CQF map lifetime checks] " + (success ? "COMPLETE" : "FAILED"));
        if (Current.Game?.Info != null) Current.Game.Info.permadeathMode = false;
        Application.Quit(success ? 0 : 1);
    }

    private int stage;
    private int checks;
    private bool finished;
    private DebugActionNode? staleCommand;
    private GameComponent_MapManager? exitingManager;
    private Map? exitingMap;
}
