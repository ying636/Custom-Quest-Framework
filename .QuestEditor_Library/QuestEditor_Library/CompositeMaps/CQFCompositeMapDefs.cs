using RimWorld;
using Verse;

namespace QuestEditor_Library;

[DefOf]
public static class CQFCompositeMapDefs
{
    static CQFCompositeMapDefs() => DefOfHelper.EnsureInitializedInCtor(typeof(CQFCompositeMapDefs));

    public static WorldObjectDef CQF_CompositeMapRegion = null!;
    public static ThingDef CQF_CompositeMapPortal = null!;
    public static JobDef CQF_CompositeMapTravel = null!;
    public static GenStepDef CQF_CompositeMapVisibility = null!;
}
