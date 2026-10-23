using UnityEngine;
using Verse;

namespace CQF.DebugMenuRuntimeChecks;

[StaticConstructorOnStartup]
public static class DebugMenuChecksStartup
{
    static DebugMenuChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfdebugmenuchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_DebugMenuChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<DebugMenuChecksDriver>();
        });
    }
}
