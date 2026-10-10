using UnityEngine;
using Verse;

namespace CQF.HibernationChecks;

[StaticConstructorOnStartup]
public static class HibernationChecksStartup
{
    static HibernationChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfhibernationchecks")) return;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject gameObject = new GameObject("CQF_HibernationChecks");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<HibernationChecksDriver>();
        });
    }
}
