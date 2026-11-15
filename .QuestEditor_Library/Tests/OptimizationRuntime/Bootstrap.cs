using UnityEngine;
using Verse;

namespace CQF.OptimizationChecks;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfoptimizationchecks")) return;
        Prefs.VolumeMaster = 0f;
        Prefs.EdgeScreenScroll = false;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            GameObject owner = new("CQF_OptimizationChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<OptimizationDriver>();
        });
    }
}
