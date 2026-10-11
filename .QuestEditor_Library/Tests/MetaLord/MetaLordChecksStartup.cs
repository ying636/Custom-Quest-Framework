using UnityEngine;
using Verse;

namespace CQF.MetaLordChecks;

[StaticConstructorOnStartup]
public static class MetaLordChecksStartup
{
    static MetaLordChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfmetalordchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject runner = new GameObject("CQF_MetaLordChecks");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<MetaLordChecksDriver>();
        });
    }
}
