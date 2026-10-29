using RimWorld;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed class CQFLayoutGenerationBatch : IDisposable
{
    public CQFLayoutGenerationBatch(Quest quest)
    {
        this.quest = quest;
        standalone = !GameTools.isGeneratingMap;
        if (!standalone) return;
        counts = GenStep_CustomMap.generatedCount; keyCounts = GenStep_CustomMap.generatedCount_Key; limits = GenStep_CustomMap.generatedLimit_Key;
        excluded = GenStep_CustomMap.disgenerate; requests = GenStep_CustomMap.requests;
        namedLords = GenStep_CustomMap.lordsWithName; lordData = GenStep_CustomMap.lordsWithData; lordTargets = GenStep_CustomMap.lordsWithTarget;
        faction = GenStep_CustomMap.faction; replacements = GenStep_CustomMap.replaceData;
        GenStep_CustomMap.generatedCount = new Dictionary<CustomMapDataDef, int>();
        GenStep_CustomMap.generatedCount_Key = new Dictionary<string, int>();
        GenStep_CustomMap.generatedLimit_Key = new Dictionary<string, int>();
        GenStep_CustomMap.disgenerate = new List<IntVec3>(); GenStep_CustomMap.requests = new List<ExecutiveRequest>();
        GenStep_CustomMap.lordsWithName = new Dictionary<string, Lord>(); GenStep_CustomMap.lordsWithData = new Dictionary<Lord, LordData>();
        GenStep_CustomMap.lordsWithTarget = new Dictionary<Lord, Dictionary<string, TargetInfo>>();
        GenStep_CustomMap.faction = null!; GenStep_CustomMap.replaceData = null!;
        GameTools.isGeneratingMap = true;
        GameTools.ResetTemporaryTargets();
    }

    public void Complete()
    {
        if (!standalone) return;
        GenStep_CustomMap.PostGenerateMap(quest);
        foreach (ExecutiveRequest request in GenStep_CustomMap.requests.ToList()) request.Execute();
    }

    public void Dispose()
    {
        if (!standalone) return;
        GameTools.ClearTemporaryTargets(); GameTools.isGeneratingMap = false;
        GenStep_CustomMap.generatedCount = counts!; GenStep_CustomMap.generatedCount_Key = keyCounts!; GenStep_CustomMap.generatedLimit_Key = limits!;
        GenStep_CustomMap.disgenerate = excluded!; GenStep_CustomMap.requests = requests!;
        GenStep_CustomMap.lordsWithName = namedLords!; GenStep_CustomMap.lordsWithData = lordData!; GenStep_CustomMap.lordsWithTarget = lordTargets!;
        GenStep_CustomMap.faction = faction!; GenStep_CustomMap.replaceData = replacements!;
    }

    private readonly Quest quest;
    private readonly bool standalone;
    private readonly Dictionary<CustomMapDataDef, int>? counts;
    private readonly Dictionary<string, int>? keyCounts;
    private readonly Dictionary<string, int>? limits;
    private readonly List<IntVec3>? excluded;
    private readonly List<ExecutiveRequest>? requests;
    private readonly Dictionary<string, Lord>? namedLords;
    private readonly Dictionary<Lord, LordData>? lordData;
    private readonly Dictionary<Lord, Dictionary<string, TargetInfo>>? lordTargets;
    private readonly Faction? faction;
    private readonly ReplaceData? replacements;
}
