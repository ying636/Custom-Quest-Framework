using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class MapComponent_CQFEcology : MapComponent
{
    public MapComponent_CQFEcology(Map map) : base(map)
    {
    }

    public BiomeDef? Biome { get => biome; set => biome = value; }

    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Defs.Look(ref biome, "CQF_EcologyBiome");
    }

    private BiomeDef? biome;
}
