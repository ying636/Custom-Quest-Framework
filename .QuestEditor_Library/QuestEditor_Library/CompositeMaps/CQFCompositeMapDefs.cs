using RimWorld;
using Verse;

namespace QuestEditor_Library;

[DefOf]
public static class CQFCompositeMapDefs
{
    static CQFCompositeMapDefs() => DefOfHelper.EnsureInitializedInCtor(typeof(CQFCompositeMapDefs));

    public static WorldObjectDef CQF_CompositeMapRegion;
    public static ThingDef CQF_CompositeEntrance;
    public static ThingDef CQF_CompositeExit;
    public static GenStepDef CQF_CompositeMapVisibility;
}
