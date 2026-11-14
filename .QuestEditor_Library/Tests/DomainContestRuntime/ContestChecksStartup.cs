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
        if (GenCommandLine.CommandLineArgPassed("cqfdomainchanges"))
        {
            foreach (DomainDef domain in DefDatabase<DomainDef>.AllDefsListForReading) domain.preload = false;
            DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain").workerClass = typeof(DomainWorker_TerrainProbe);
        }
        if (GenCommandLine.CommandLineArgPassed("cqfcontiguouslabchecks"))
            foreach (DomainDef domain in DefDatabase<DomainDef>.AllDefsListForReading)
                domain.preload = ContiguousDomainChecksDriver.DomainNames.Contains(domain.defName) || domain.defName is "CQF_TestDomain" or "RSH_BloodSeaDomain" or "CQF_CheckPlainDomain";
        if (GenCommandLine.CommandLineArgPassed("cqfcounterrepair")) CounterTransitionTimingChecks.Install();
        if (GenCommandLine.CommandLineArgPassed("cqfcounterrepair")) CounterPreparationSurfaceChecks.Install();
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
            if (GenCommandLine.CommandLineArgPassed("cqfdomainchanges")) owner.AddComponent<DomainChangesChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfcontiguouslabchecks")) owner.AddComponent<ContiguousDomainChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfgroupchecks")) owner.AddComponent<DomainGroupChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfmultiplechecks")) owner.AddComponent<MultipleDomainChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfreasonchecks")) owner.AddComponent<DomainReasonChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfbattlefieldchecks")) owner.AddComponent<BattlefieldChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfclosingcarrychecks")) owner.AddComponent<ClosingCarryChecksDriver>();
            else if (GenCommandLine.CommandLineArgPassed("cqfstartupchecks")) owner.AddComponent<StartupChecksDriver>();
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
