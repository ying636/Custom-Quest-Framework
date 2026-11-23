using Verse;

namespace QuestEditor_Library;

public sealed class GenStep_CompositeMapVisibility : GenStep
{
    public override int SeedPart => 1972461563;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (map.Parent is CQFCompositeMapParent { mapDataDef.fogged: false })
            map.fogGrid.ClearAllFog();
    }
}
