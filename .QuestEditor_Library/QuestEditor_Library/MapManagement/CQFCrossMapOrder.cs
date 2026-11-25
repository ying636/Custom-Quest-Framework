#if RIMWORLD_1_7
using Verse;

namespace QuestEditor_Library;

internal enum CQFCrossMapOrderKind
{
    Move,
    Pursuit,
    Report,
    Reinforcement,
    Haul
}

internal sealed class CQFCrossMapOrder : IExposable
{
    public void ExposeData()
    {
        Scribe_References.Look(ref subject!, "subject");
        Scribe_References.Look(ref current!, "current");
        Scribe_References.Look(ref destination!, "destination");
        Scribe_References.Look(ref origin!, "origin");
        Scribe_References.Look(ref reportOrigin, "reportOrigin");
        Scribe_References.Look(ref quarry, "quarry");
        Scribe_Values.Look(ref cell, "cell");
        Scribe_Values.Look(ref preferredExit, "preferredExit", IntVec3.Invalid);
        Scribe_Values.Look(ref rotation, "rotation");
        Scribe_Values.Look(ref kind, "kind");
        Scribe_Values.Look(ref dueTick, "dueTick");
        Scribe_Values.Look(ref expires, "expires");
        Scribe_Values.Look(ref marchUntil, "marchUntil");
        Scribe_Values.Look(ref responders, "responders");
        Scribe_Values.Look(ref reinforcementGroupTag!, "reinforcementGroupTag", "");
        Scribe_Values.Look(ref assemblyTicks, "assemblyTicks");
        Scribe_Values.Look(ref delivered, "delivered");
    }

    public Thing subject = null!;
    public Map current = null!;
    public Map destination = null!;
    public Map origin = null!;
    public Map? reportOrigin;
    public Pawn? quarry;
    public IntVec3 cell;
    public IntVec3 preferredExit = IntVec3.Invalid;
    public Rot4 rotation;
    public CQFCrossMapOrderKind kind;
    public int dueTick;
    public int expires;
    public int marchUntil;
    public int responders;
    public string reinforcementGroupTag = "";
    public int assemblyTicks;
    public bool delivered;
    public int scheduled;
    public bool reachedDeparture;
}
#endif
