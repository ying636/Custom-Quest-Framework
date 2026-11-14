using System.Text.Json;
using RimWorld;
using Verse;

namespace CQF.RuntimeChecks;

[StaticConstructorOnStartup]
public static class RuntimeProcessRecord
{
    static RuntimeProcessRecord()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfmaplayoutchecks") && !GenCommandLine.CommandLineArgPassed("cqfdialogpresentationchecks")) return;
        if (!GenCommandLine.TryGetCommandLineArg("cqfprojectionoutput", out string path)) return;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "Process.json"), JsonSerializer.Serialize(new
        {
            PID = Environment.ProcessId, ExecutablePath = Environment.ProcessPath, CommandLine = Environment.GetCommandLineArgs(),
            SaveData = GenFilePaths.SaveDataFolderPath, SteamAppId = Environment.GetEnvironmentVariable("SteamAppId"),
            SteamGameId = Environment.GetEnvironmentVariable("SteamGameId"), SteamOverlayGameId = Environment.GetEnvironmentVariable("SteamOverlayGameId"),
            Version = VersionControl.CurrentVersionStringWithRev
        }, new JsonSerializerOptions { WriteIndented = true }));
        Prefs.VolumeMaster = 0f;
        Prefs.EdgeScreenScroll = false;
    }
}
