using System.Text.Json;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.PanoramaLightingChecks;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfpanoramachecks")) return;
        Directory.CreateDirectory(OutputDirectory);
        WriteJson("Process.json", new
        {
            PID = Environment.ProcessId, ExecutablePath = Environment.ProcessPath,
            CommandLine = Environment.GetCommandLineArgs(), SaveData = GenFilePaths.SaveDataFolderPath,
            SteamAppId = Environment.GetEnvironmentVariable("SteamAppId"),
            SteamGameId = Environment.GetEnvironmentVariable("SteamGameId"),
            SteamOverlayGameId = Environment.GetEnvironmentVariable("SteamOverlayGameId"),
            Version = VersionControl.CurrentVersionStringWithRev
        });
        Prefs.VolumeMaster = 0f;
        Prefs.EdgeScreenScroll = false;
        GameObject host = new("CQF.PanoramaLightingChecks");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<PanoramaChecksDriver>();
    }

    public static string OutputDirectory => GenCommandLine.TryGetCommandLineArg("cqfpanoramaoutput", out string path)
        ? Path.GetFullPath(path) : throw new InvalidOperationException("cqfpanoramaoutput");

    public static void WriteJson(string name, object value)
    {
        File.WriteAllText(Path.Combine(OutputDirectory, name), JsonSerializer.Serialize(value,
            new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
    }
}
