using System.Text;
using System.Text.Json;
using HarmonyLib;
using LudeonTK;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DebugMenuRuntimeChecks;

public sealed class DebugMenuChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "DebugMenuChecks.txt");

    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 180f) throw new TimeoutException("Debug menu checks did not reach the requested state.");
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            bool playing = GenCommandLine.CommandLineArgPassed("quicktest");
            if (playing && (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl)) return;
            File.WriteAllText(Results, "TRACE Debug menu initialization with current game=" + (Current.Game != null) + "\n", Encoding.UTF8);
            if (GenCommandLine.CommandLineArgPassed("cqfexpectbroken")) Reproduce();
            else Validate(playing);
            Finish(true);
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, "FAIL " + error + "\n", Encoding.UTF8);
            Log.Error("[CQF debug menu checks] " + error);
            Finish(false);
        }
    }

    private void Reproduce()
    {
        Check(Current.Game == null, "the baseline begins at the main menu without a game");
        Dialog_Debug.ResetStaticData();
        Exception? first = null;
        try { Dialog_Debug.TrySetupNodeGraph(); }
        catch (Exception error) { first = error; }
        Check(first != null && first.ToString().Contains("MapHibernation", StringComparison.Ordinal), "the first initialization failure originates in CQF MapHibernation");
        File.AppendAllText(Results, "EXPECTED ORIGINAL " + first + "\n", Encoding.UTF8);
        Exception? second = null;
        try { _ = new Dialog_Debug(DefDatabase<DebugTabMenuDef>.GetNamed("Settings")); }
        catch (Exception error) { second = error; }
        Check(second is KeyNotFoundException && second.Message.Contains("Settings", StringComparison.Ordinal), "the incomplete root graph reproduces the reported Settings KeyNotFoundException");
        Dialog_Debug.ResetStaticData();
    }

    private void Validate(bool playing)
    {
        Check(playing == (Current.Game != null), "the native runtime reached the requested menu or game state");
        DebugActionNode hibernation = DebugActions_MapHibernation.MapHibernation();
        Check(hibernation.childGetter != null && hibernation.children.Count == 0, "constructing the hibernation menu defers all game access");
        Dialog_Debug.ResetStaticData();
        Dialog_Debug.TrySetupNodeGraph();
        int expected = DefDatabase<DebugTabMenuDef>.AllDefsListForReading.Count;
        var roots = (Dictionary<DebugTabMenuDef, DebugActionNode>)AccessTools.Field(typeof(Dialog_Debug), "roots").GetValue(null)!;
        Check(roots.Count == expected && roots.ContainsKey(DefDatabase<DebugTabMenuDef>.GetNamed("Settings")), "all native debug tabs including Settings initialize completely");
        DebugActionNode initialRoot = Dialog_Debug.rootNode;
        Dialog_Debug.TrySetupNodeGraph();
        Check(ReferenceEquals(initialRoot, Dialog_Debug.rootNode) && roots.Count == expected, "repeated initialization keeps the complete cached graph");
        foreach (DebugTabMenuDef tab in DefDatabase<DebugTabMenuDef>.AllDefsListForReading)
        {
            Dialog_Debug dialog = new(tab);
            Check(dialog.CurrentNode != null, "native debug tab opens: " + tab.defName);
            dialog.Close(false);
        }
        if (playing)
        {
            GameComponent_MapManager manager = GameComponent_MapManager.Instance ?? throw new InvalidOperationException("Map manager is missing.");
            List<DebugActionNode> actions = (hibernation.childGetter ?? throw new InvalidOperationException("The map submenu is not deferred."))();
            Game game = Current.Game ?? throw new InvalidOperationException("The test game is missing.");
            Check(actions.Count == game.Maps.Count + manager.HibernatingMaps.Count(), "opening the map submenu enumerates the current active and sleeping maps");
            Check(actions.All(action => action.action != null), "the delayed submenu retains its real hibernate and activate callbacks");
            DebugActionNode fresh = DebugActions_MapHibernation.MapHibernation();
            Check(fresh.childGetter != null && fresh.children.Count == 0, "a new menu request does not reuse a prior game map snapshot");
        }
        DebugWindowsOpener opener = new();
        var toggle = AccessTools.Method(typeof(DebugWindowsOpener), "ToggleDebugSettingsMenu");
        toggle.Invoke(opener, null);
        Check(Find.WindowStack.Windows.OfType<Dialog_Debug>().Any(), "the reported ToggleDebugSettingsMenu entry point opens the native window");
        Dialog_Debug settings = Find.WindowStack.Windows.OfType<Dialog_Debug>().Single();
        toggle.Invoke(opener, null);
        Check(Find.WindowStack.Windows.OfType<Dialog_Debug>().Single() == settings, "reopening settings reuses its valid native window");
        settings.Close(false);
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(Results, "PASS " + ++checks + " " + message + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(Root, "DebugMenuEvidence.json"), JsonSerializer.Serialize(new { success, checks,
            gameVersion = VersionControl.CurrentVersionString, core = typeof(DebugActions_MapHibernation).Assembly.Location },
            new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        if (Current.Game?.Info != null) Current.Game.Info.permadeathMode = false;
        Application.Quit(success ? 0 : 1);
    }

    private int checks;
    private bool finished;
}
