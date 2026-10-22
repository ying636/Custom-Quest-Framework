using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicTarget : IExposable
{
    public CQFCinematicTarget() { }

    public CQFCinematicTarget(string key, TargetInfo target)
    {
        this.key = key;
        thing = target.Thing;
        cell = target.Cell;
        map = target.Map;
    }

    public TargetInfo Target => thing != null ? new TargetInfo(thing)
        : map != null && cell.IsValid ? new TargetInfo(cell, map) : TargetInfo.Invalid;

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key", "");
        Scribe_Values.Look(ref cell, "cell", IntVec3.Invalid);
        Scribe_References.Look(ref thing, "thing", true);
        Scribe_References.Look(ref map, "map");
    }

    public string key = "";
    private Thing? thing;
    private Map? map;
    private IntVec3 cell = IntVec3.Invalid;
}
