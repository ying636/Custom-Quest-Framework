using UnityEngine;
using Verse;
using QuestEditor_Library;

namespace CQF.DomainContestRuntimeChecks;

[StaticConstructorOnStartup]
public static class ContestChecksStartup
{
    static ContestChecksStartup()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfcontestchecks")) return;
        if (GenCommandLine.CommandLineArgPassed("cqfstartupchecks"))
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"),
                "TRACE startup constructors reached at " + Time.realtimeSinceStartup + " s\n");
            StartupChecksProfiler.Install();
        }
        if (GenCommandLine.CommandLineArgPassed("cqfskippreload"))
            foreach (DomainDef domain in DefDatabase<DomainDef>.AllDefsListForReading) domain.preload = false;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject owner = new("CQF_DomainContestChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            if (GenCommandLine.CommandLineArgPassed("cqfstartupchecks")) owner.AddComponent<StartupChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfreviewfixchecks")) owner.AddComponent<ReviewFixesChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfcounteropeningchecks")) owner.AddComponent<CounterOpeningChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfoccupationchecks")) owner.AddComponent<OccupationChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfbattlerulechecks")) owner.AddComponent<BattleRulesChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqftrappingchecks")) owner.AddComponent<TrappingChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfentrychecks")) owner.AddComponent<EntryChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqffusionchecks")) owner.AddComponent<OpeningFusionChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqflayoutchecks")) owner.AddComponent<LayoutChecksDriver>();
            else owner.AddComponent<ContestChecksDriver>();
        });
    }
}
