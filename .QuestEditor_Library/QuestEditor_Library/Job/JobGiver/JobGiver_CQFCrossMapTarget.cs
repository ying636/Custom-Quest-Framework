#if RIMWORLD_1_7
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public abstract class JobGiver_CQFCrossMapTarget : JobGiver_TargetBase
{
    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var copy = (JobGiver_CQFCrossMapTarget)base.DeepCopy(resolve);
        copy.compositeMapKey = compositeMapKey;
        copy.compositeMapLookup = compositeMapLookup;
        copy.currentCompositeGroupOnly = currentCompositeGroupOnly;
        copy.targetKey = targetKey;
        copy.useRuntimeDatabase = useRuntimeDatabase;
        copy.useQuestDatabase = useQuestDatabase;
        copy.useTemporaryDatabase = useTemporaryDatabase;
        copy.useGlobalDatabase = useGlobalDatabase;
        return copy;
    }

    protected Map? ResolveDestination(Pawn pawn, GameComponent_MapManager manager)
    {
        if (string.IsNullOrWhiteSpace(compositeMapKey))
            return TryGetTarget(pawn, out TargetInfo target) ? manager.ResolveCrossMapTarget(target) : null;
        if (manager.GetCompositeMap(pawn.Map) is not { } instance) return null;
        CQFCompositeMapGroup? group = currentCompositeGroupOnly ? instance.GetGroup(pawn.Map) : null;
        return instance.MapDatabase.Find(compositeMapKey, compositeMapLookup, group);
    }

    [NoTranslate]
    public string compositeMapKey = "";
    public CQFCompositeMapLookup compositeMapLookup;
    public bool currentCompositeGroupOnly;
}
#endif
