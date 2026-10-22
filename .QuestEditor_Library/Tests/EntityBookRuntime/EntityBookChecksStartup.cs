using UnityEngine;
using Verse;

namespace CQF.EntityBookRuntimeChecks;

[StaticConstructorOnStartup]
public static class EntityBookChecksStartup
{
    static EntityBookChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfentitybookchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_EntityBookRuntimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<EntityBookChecksDriver>();
        });
    }
}
