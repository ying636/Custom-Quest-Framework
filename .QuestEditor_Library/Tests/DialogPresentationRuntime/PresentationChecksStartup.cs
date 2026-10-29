using UnityEngine;
using Verse;

namespace CQF.DialogPresentationChecks;

[StaticConstructorOnStartup]
public static class PresentationChecksStartup
{
    static PresentationChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfdialogpresentationchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_DialogPresentationChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<PresentationChecksDriver>();
        });
    }
}
