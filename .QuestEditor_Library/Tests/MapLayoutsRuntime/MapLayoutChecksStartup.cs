using UnityEngine;
using Verse;

namespace CQF.MapLayoutRuntimeChecks;

[StaticConstructorOnStartup]
public static class MapLayoutChecksStartup
{
    static MapLayoutChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfmaplayoutchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new GameObject("CQF_MapLayoutChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<MapLayoutChecksDriver>();
        });
    }
}
