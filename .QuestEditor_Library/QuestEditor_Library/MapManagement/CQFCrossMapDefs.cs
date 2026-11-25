#if RIMWORLD_1_7
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[DefOf]
internal static class CQFCrossMapDefs
{
    static CQFCrossMapDefs() => DefOfHelper.EnsureInitializedInCtor(typeof(CQFCrossMapDefs));

    public static JobDef CQF_CrossMapWalk;
    public static JobDef CQF_CrossMapHaul;
}
#endif
