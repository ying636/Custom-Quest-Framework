using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace CQF.CorePerformanceChecks;

public sealed class PerformanceSpawnData : PawnSpawnData
{
    public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null!, bool setLord = true)
    {
        Calls++;
        LastMap = map;
        LastPosition = position;
        Callback?.Invoke();
        return null!;
    }

    public int Calls;
    public Map? LastMap;
    public IntVec3 LastPosition;
    public Action? Callback;
}
