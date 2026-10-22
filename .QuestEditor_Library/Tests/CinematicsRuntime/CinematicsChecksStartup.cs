using UnityEngine;
using Verse;

namespace CQF.CinematicsRuntimeChecks;

[StaticConstructorOnStartup]
public static class CinematicsChecksStartup
{
    static CinematicsChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfcinematicchecks")) return;
        CinematicsCheckDefinitions.Register();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_CinematicsRuntimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<CinematicsChecksDriver>();
        });
    }
}
