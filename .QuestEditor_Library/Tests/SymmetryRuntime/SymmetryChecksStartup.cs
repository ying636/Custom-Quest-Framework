using UnityEngine;
using Verse;

namespace CQF.SymmetryChecks;

[StaticConstructorOnStartup]
public static class SymmetryChecksStartup
{
    static SymmetryChecksStartup()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject runner = new("CQF_SymmetryChecks");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SymmetryChecksDriver>();
        });
    }

    public static void Start() { }
}
