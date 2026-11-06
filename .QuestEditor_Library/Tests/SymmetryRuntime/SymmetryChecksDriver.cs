using System.Reflection;
using System.Text;
using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace CQF.SymmetryChecks;

public sealed class SymmetryChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (this.finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 360f) throw new TimeoutException("Symmetry checks timed out.");
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            if (this.stage == 0)
            {
                if (!GenScene.InEntryScene) return;
                typeof(QuickStarter).GetField("quickStarted", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, true);
                this.stage = 1;
                SceneManager.LoadScene("Play");
                return;
            }
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            DebugSettings.pauseOnError = false;
            if (this.stage == 1)
            {
                this.Check(CQFEditorBridge.IsLoaded, "editor module is enabled in the isolated game");
                this.Check(!Verse.Steam.SteamManager.Initialized, "runtime is isolated from Steam initialization");
                Prefs.DevMode = true;
                this.oldEdgeScroll = Prefs.EdgeScreenScroll;
                Prefs.EdgeScreenScroll = false;
                DebugSettings.godMode = true;
                QuestEditor_CustomQuestMap.GenerateMap(new IntVec3(81, 1, 81));
                this.stage = 2;
                return;
            }
            if (this.stage == 2)
            {
                Map map = Find.CurrentMap;
                if (map.Size.x != 81) return;
                new SymmetryFunctionalChecks(map, this.Check).Run();
                Find.CameraDriver.SetRootPosAndSize(new Vector3(40.5f, 0f, 40.5f), 45f);
                foreach (Window window in Find.WindowStack.Windows.Where(window => window.absorbInputAroundWindow).ToArray()) window.Close(false);
                this.dragChecks = new SymmetryAxisDragChecks(map, this.Check);
                this.symmetryGui = CQFEditorBridge.SymmetryOnGUI;
                CQFEditorBridge.SymmetryOnGUI = this.CheckAxisInput;
                this.stage = 9;
                return;
            }
            if (this.stage == 9 && this.dragChecks!.Complete)
            {
                this.SetLanguage("English");
                new Designator_CQFSymmetry().ProcessInput(new Event { type = EventType.MouseDown, button = 0 });
                this.dialog = Find.WindowStack.WindowOfType<Dialog_CQFSymmetry>();
                this.Check(this.dialog != null, "the Orders symmetry command opens its settings window");
                this.stage = 3;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 3 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                this.CloseLogs();
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Symmetry-English.png"));
                this.stage = 4;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 4 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                this.dialog!.Close(false);
                this.SetLanguage("ChineseSimplified (简体中文)");
                this.dialog = new Dialog_CQFSymmetry(Find.CurrentMap);
                Find.WindowStack.Add(this.dialog);
                this.stage = 5;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 5 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                this.CloseLogs();
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Symmetry-Chinese.png"));
                this.stage = 6;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 6 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                this.dialog!.Close(false);
                Find.DesignatorManager.Deselect();
                Find.CameraDriver.SetRootPosAndSize(new Vector3(40.5f, 0f, 40.5f), 45f);
                this.stage = 10;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 10 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Symmetry-MapHandles.png"));
                this.stage = 11;
                this.wait = Time.realtimeSinceStartup;
                return;
            }
            if (this.stage == 11 && Time.realtimeSinceStartup - this.wait > 1f)
            {
                this.profiler = new SymmetryProfilerChecks(Find.CurrentMap, this.Check);
                this.stage = 7;
            }
            if (this.stage == 7 && this.profiler!.Update())
            {
                this.profiler.Restore();
                CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(Find.CurrentMap);
                symmetry.enabled = true;
                symmetry.mirrorX = true;
                symmetry.mirrorZ = true;
                symmetry.axisX = 79;
                symmetry.axisZ = 80;
                this.savedMap = Find.CurrentMap.GetUniqueLoadID();
                Current.Game.Info.permadeathMode = false;
                GameDataSaveLoader.SaveGame("CQF_SymmetryRuntime");
                this.Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_SymmetryRuntime")), "a real native game save is written");
                symmetry.enabled = false;
                symmetry.axisX = 2;
                this.stage = 8;
                GameDataSaveLoader.LoadGame("CQF_SymmetryRuntime");
                return;
            }
            if (this.stage == 8)
            {
                Map map = Find.Maps.Single(value => value.GetUniqueLoadID() == this.savedMap);
                CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(map);
                this.Check(symmetry.enabled && symmetry.mirrorX && symmetry.mirrorZ && symmetry.axisX == 79 && symmetry.axisZ == 80,
                    "reloading a complete game restores the independent symmetry settings on the editor map");
                this.Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SymmetryChecks.txt"), "FAIL " + error + "\n", Encoding.UTF8);
            Log.Error("[CQF symmetry checks] " + error);
            this.Finish(false);
        }
    }

    private void CheckAxisInput(Map map)
    {
        if (this.finished || this.stage != 9 || Event.current.type != EventType.Repaint)
        {
            this.symmetryGui?.Invoke(map);
            return;
        }
        CQFEditorBridge.SymmetryOnGUI = this.symmetryGui;
        try { this.dragChecks!.RunOnGUI(); }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SymmetryChecks.txt"), "FAIL " + error + "\n", Encoding.UTF8);
            Log.Error("[CQF symmetry drag checks] " + error);
            this.Finish(false);
        }
    }

    private void SetLanguage(string folder)
    {
        LoadedLanguage language = LanguageDatabase.AllLoadedLanguages.First(value => value.folderName == folder);
        LanguageDatabase.activeLanguage = language;
        language.LoadMetadata();
        language.LoadData();
        this.Check("CQF_SymmetryTitle".Translate().ToString() != "CQF_SymmetryTitle", "symmetry translation resolves in " + folder);
    }

    private void CloseLogs()
    {
        foreach (Window window in Find.WindowStack.Windows.Where(window => window.GetType().Name == "EditWindow_Log").ToArray()) window.Close(false);
    }

    private void Check(bool passed, string text)
    {
        if (!passed) throw new InvalidOperationException(text);
        this.checks++;
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SymmetryChecks.txt"), "PASS " + this.checks + " " + text + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        this.finished = true;
        this.profiler?.Restore();
        if (this.symmetryGui != null) CQFEditorBridge.SymmetryOnGUI = this.symmetryGui;
        if (this.oldEdgeScroll.HasValue) Prefs.EdgeScreenScroll = this.oldEdgeScroll.Value;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SymmetryEvidence.json"), JsonSerializer.Serialize(new
        {
            success, this.checks, gameVersion = VersionControl.CurrentVersionString,
            core = typeof(CQFEditorBridge).Assembly.Location, editor = typeof(Designator_CQFSymmetry).Assembly.Location,
            mods = LoadedModManager.RunningMods.Select(mod => mod.PackageIdPlayerFacing).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        Application.Quit(success ? 0 : 1);
    }

    private int stage;
    private int checks;
    private bool finished;
    private float wait;
    private Dialog_CQFSymmetry? dialog;
    private SymmetryAxisDragChecks? dragChecks;
    private SymmetryProfilerChecks? profiler;
    private string? savedMap;
    private bool? oldEdgeScroll;
    private Action<Map>? symmetryGui;
}
