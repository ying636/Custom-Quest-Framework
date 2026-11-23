using RimWorld;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeEdgeMap
{
    internal CQFCompositeEdgeMap(Map map)
    {
        this.Map = map;
        this.Size = map.Size;
        this.PlayerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
    }

    internal Map Map { get; }
    internal IntVec3 Size { get; }
    internal List<Pawn> PlayerPawns { get; }
    internal HashSet<IntVec3> Cells { get; } = new();
    internal int NextCheckTick { get; set; }
}
