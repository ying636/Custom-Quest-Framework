using System.Text.Json;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.ProjectionModuleChecks;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfprojectionchecks")) return;
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "Process.json"), JsonSerializer.Serialize(new
        {
            PID = Environment.ProcessId, ExecutablePath = Environment.ProcessPath, CommandLine = Environment.GetCommandLineArgs(),
            SaveData = GenFilePaths.SaveDataFolderPath, SteamAppId = Environment.GetEnvironmentVariable("SteamAppId"),
            SteamGameId = Environment.GetEnvironmentVariable("SteamGameId"), SteamOverlayGameId = Environment.GetEnvironmentVariable("SteamOverlayGameId"),
            Version = VersionControl.CurrentVersionStringWithRev
        }, new JsonSerializerOptions { WriteIndented = true }));
        Prefs.VolumeMaster = 0f;
        Prefs.EdgeScreenScroll = false;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            GameObject owner = new("CQF_ProjectionModuleChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<ModuleChecksDriver>();
        });
    }

    public static string Output => GenCommandLine.TryGetCommandLineArg("cqfprojectionoutput", out string value)
        ? value : throw new InvalidOperationException("cqfprojectionoutput");
}
