using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.SaveCompatibilityRuntimeChecks;

[StaticConstructorOnStartup]
public static class SaveCompatibilityStartup
{
    static SaveCompatibilityStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfsavecompatibility")) return;
        foreach (string name in new[] { "CQF_CompatibilityUnique", "CQF_CompatibilityLaterCache" })
        {
            ComplexPawnDef template = new() { defName = name, label = name };
            PawnModData_Basic basic = template.DataFor<PawnModData_Basic>();
            basic.kindDef = DefDatabase<PawnKindDef>.GetNamed("Colonist");
            basic.unique = true;
            DefDatabase<ComplexPawnDef>.Add(template);
        }
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_SaveCompatibilityRuntimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<SaveCompatibilityDriver>();
        });
    }
}
