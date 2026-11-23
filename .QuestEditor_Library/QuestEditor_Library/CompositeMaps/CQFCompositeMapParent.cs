using Verse;

namespace QuestEditor_Library;

public sealed class CQFCompositeMapParent : MapParent_Custom
{
    public override string MapName => mapDataDef != null && mapDataDef.defName.CanTranslate()
        ? mapDataDef.defName.Translate() : base.MapName;

    protected override bool RefogAfterMapGeneration => mapDataDef.fogged;
    internal bool GenerationCompleted { get; set; }
}
