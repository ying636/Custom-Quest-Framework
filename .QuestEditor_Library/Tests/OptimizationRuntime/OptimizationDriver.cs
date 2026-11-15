using System.Reflection;
using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.OptimizationChecks;

public sealed partial class OptimizationDriver : MonoBehaviour
{
    public static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "OptimizationChecks.txt"), "PASS " + name + "\n");
    }

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            if (!started)
            {
                started = true;
                Find.TickManager.Pause();
                File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "Process.json"), JsonSerializer.Serialize(new
                {
                    PID = Environment.ProcessId, Executable = Environment.ProcessPath, Version = VersionControl.CurrentVersionStringWithRev,
                    SaveData = GenFilePaths.SaveDataFolderPath, Mods = LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId).ToArray()
                }));
                if (GenCommandLine.CommandLineArgPassed("cqfoptimizationreload"))
                {
                    this.CheckHistory(true);
                    this.Complete();
                    return;
                }
                this.CheckHistory(false);
                this.CheckEventAreas();
                this.CheckDutyRuntime();
                this.CheckQuestBookDraft();
                this.CheckLighting();
                this.CheckDomainTicks();
                selector = new OptimizationSelectorWindow();
                Find.WindowStack.Add(selector);
                return;
            }
            if (selector?.Finished != true) return;
            if (selector.Error != null) throw selector.Error;
            selector.Close();
            this.Complete();
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "OptimizationChecks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.OptimizationChecks] " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private void Complete()
    {
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "OptimizationChecks.txt"), "COMPLETE\n");
        finished = true;
        Application.Quit(0);
    }

    private static object? Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner);

    private bool started;
    private bool finished;
    private OptimizationSelectorWindow? selector;
}
