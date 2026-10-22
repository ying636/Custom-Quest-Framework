using UnityEngine;
using Verse;

namespace CQF.CinematicStudioChecks;

[StaticConstructorOnStartup]
public static class StudioChecksStartup
{
    static StudioChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfstudiochecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_CinematicStudioChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<StudioChecksDriver>();
        });
    }
}
