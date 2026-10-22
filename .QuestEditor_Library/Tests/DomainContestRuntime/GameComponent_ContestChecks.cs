using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class GameComponent_ContestChecks : GameComponent
{
    public GameComponent_ContestChecks(Game game) { }

    public int StructureId => structureId;
    public int StructureHitPoints => structureHitPoints;
    public float SavedFirstHealth => savedFirstHealth;
    public float SavedSecondHealth => savedSecondHealth;
    public int TrappingCase { get; set; }
    public int BattleRulesPhase { get; set; }
    public int OccupationPhase { get; set; }
    public int CounterOpeningCase { get; set; }

    public void RecordStructure(Thing thing)
    {
        structureId = thing.thingIDNumber;
        structureHitPoints = thing.HitPoints;
    }

    public void Record(int first, int second)
    {
        firstId = first;
        secondId = second;
    }

    public void RecordHealth(float first, float second)
    {
        savedFirstHealth = first;
        savedSecondHealth = second;
    }

    public override void StartedNewGame()
    {
        if (GenCommandLine.CommandLineArgPassed("cqfstartupchecks"))
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"),
                "TRACE main map ready at " + Time.realtimeSinceStartup + " s\n");
    }

    public override void LoadedGame()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks") || firstId == 0 || secondId == 0) return;
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"), "TRACE LoadedGame " + firstId + " " + secondId + "\n");
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"), "TRACE Resume callback\n");
            GameObject owner = new("CQF_ContestChecksResumed");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            if (GenCommandLine.CommandLineArgPassed("cqfcounteropeningchecks")) owner.AddComponent<CounterOpeningChecksDriver>().Resume(firstId, secondId, CounterOpeningCase);
            else if (GenCommandLine.CommandLineArgPassed("cqfoccupationchecks")) owner.AddComponent<OccupationChecksDriver>().Resume(firstId, secondId, OccupationPhase);
            else if (GenCommandLine.CommandLineArgPassed("cqfbattlerulechecks")) owner.AddComponent<BattleRulesChecksDriver>().Resume(firstId, secondId);
            else if (GenCommandLine.CommandLineArgPassed("cqftrappingchecks")) owner.AddComponent<TrappingChecksDriver>().Resume(firstId, secondId, TrappingCase);
            else if (GenCommandLine.CommandLineArgPassed("cqflayoutchecks")) owner.AddComponent<LayoutChecksDriver>().Resume(firstId, secondId);
            else owner.AddComponent<ContestChecksDriver>().Resume(firstId, secondId);
        });
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref firstId, "firstId");
        Scribe_Values.Look(ref secondId, "secondId");
        Scribe_Values.Look(ref savedFirstHealth, "savedFirstHealth");
        Scribe_Values.Look(ref savedSecondHealth, "savedSecondHealth");
        Scribe_Values.Look(ref structureId, "structureId");
        Scribe_Values.Look(ref structureHitPoints, "structureHitPoints");
        int trappedCase = TrappingCase;
        Scribe_Values.Look(ref trappedCase, "trappingCase");
        TrappingCase = trappedCase;
        int battlePhase = BattleRulesPhase;
        Scribe_Values.Look(ref battlePhase, "battleRulesPhase");
        BattleRulesPhase = battlePhase;
        int occupationPhase = OccupationPhase;
        Scribe_Values.Look(ref occupationPhase, "occupationPhase");
        OccupationPhase = occupationPhase;
        int counterCase = CounterOpeningCase;
        Scribe_Values.Look(ref counterCase, "counterOpeningCase");
        CounterOpeningCase = counterCase;
        if (GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"), "TRACE Component " + Scribe.mode + " " + firstId + " " + secondId + "\n");
    }

    private int firstId;
    private int secondId;
    private float savedFirstHealth;
    private float savedSecondHealth;
    private int structureId;
    private int structureHitPoints;
}
