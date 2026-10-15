using UnityEngine;
using Verse;

namespace CQF.DomainRuntimeChecks;

[StaticConstructorOnStartup]
public static class DomainChecksStartup
{
    static DomainChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfdomainchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_DomainRuntimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<DomainChecksDriver>();
        });
    }
}
