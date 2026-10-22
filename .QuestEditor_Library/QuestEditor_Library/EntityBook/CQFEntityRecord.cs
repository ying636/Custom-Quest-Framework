using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEntityRecord : IExposable
{
    public CQFEntityRecord() { }

    public CQFEntityRecord(string sourceId, string sourceLabel, int memberIndex, Pawn pawn, Quest? quest, Map? map)
    {
        this.sourceId = sourceId;
        this.sourceLabel = sourceLabel;
        this.memberIndex = memberIndex;
        this.pawn = pawn;
        questId = quest?.id ?? -1;
        thingId = pawn.GetUniqueLoadID();
        mapId = (pawn.MapHeld ?? map)?.uniqueID ?? -1;
    }

    public string SourceId => sourceId;
    public string SourceLabel => sourceLabel;
    public int MemberIndex => memberIndex;
    public Pawn? Pawn => pawn;
    public int QuestId => questId;
    public int MapId => pawn?.MapHeld?.uniqueID ?? mapId;
    public string ThingId => thingId;
    public bool IsAvailable => pawn != null && !pawn.Destroyed && !pawn.Dead;

    public void Refresh(Pawn value, Map? map, string? label)
    {
        pawn = value;
        mapId = (value.MapHeld ?? map)?.uniqueID ?? mapId;
        if (!string.IsNullOrWhiteSpace(label)) sourceLabel = label!;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref sourceId, "sourceId", "");
        Scribe_Values.Look(ref sourceLabel, "sourceLabel", "");
        Scribe_Values.Look(ref memberIndex, "memberIndex");
        Scribe_Values.Look(ref questId, "questId", -1);
        Scribe_Values.Look(ref mapId, "mapId", -1);
        Scribe_Values.Look(ref thingId, "thingId", "");
        Scribe_References.Look(ref pawn, "pawn");
    }

    private string sourceId = "";
    private string sourceLabel = "";
    private int memberIndex;
    private int questId = -1;
    private int mapId = -1;
    private string thingId = "";
    private Pawn? pawn;
}
