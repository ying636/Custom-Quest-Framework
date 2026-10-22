using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStepState : IExposable
{
    public CQFCinematicStepState Child(int index)
    {
        while (children.Count <= index) children.Add(new CQFCinematicStepState());
        return children[index];
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref entered, "entered");
        Scribe_Values.Look(ref completed, "completed");
        Scribe_Values.Look(ref elapsed, "elapsed");
        Scribe_Values.Look(ref cursor, "cursor");
        Scribe_Values.Look(ref intValue, "intValue");
        Scribe_Values.Look(ref number, "number");
        Scribe_Values.Look(ref vector, "vector");
        Scribe_Values.Look(ref color, "color");
        Scribe_Values.Look(ref text, "text", "");
        Scribe_Values.Look(ref flag, "flag");
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Collections.Look(ref children, "children", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) children ??= new();
    }

    public bool entered;
    public bool completed;
    public float elapsed;
    public int cursor;
    public int intValue;
    public float number;
    public Vector3 vector;
    public Color color;
    public string text = "";
    public bool flag;
    public Pawn? pawn;
    public List<CQFCinematicStepState> children = new();
    [Unsaved]
    public object? runtime;
}
