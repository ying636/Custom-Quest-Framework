using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class StartupChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
        bool skip = GenCommandLine.CommandLineArgPassed("cqfskippreload");
        if (!skip && DefDatabase<DomainDef>.AllDefsListForReading.Where(def => def.preload)
            .Any(def => runtime.GetMaps(def).Count() < 2 || !runtime.HasTerrainTemplate(def))) return;
        finished = true;
        string results = Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
        File.AppendAllText(results, "PASS startup ready, skipPreload=" + skip + ", elapsed=" + Time.realtimeSinceStartup
            + " s, domainMaps=" + runtime.DomainMaps.Count + "\n");
        if (GenCommandLine.CommandLineArgPassed("cqfassetchecks"))
        {
            gameObject.AddComponent<AssetChecksDriver>();
            return;
        }
        File.AppendAllText(results, "COMPLETE\n");
        Application.Quit();
    }

    private bool finished;
}
