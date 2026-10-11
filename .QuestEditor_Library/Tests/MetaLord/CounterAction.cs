using System.Collections.Generic;
using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.MetaLordChecks;

public sealed class CounterAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Counts.TryGetValue(key, out int count);
        Counts[key] = count + 1;
        if (repeat) Find.SignalManager.SendSignal(new Signal(key));
    }

    public override void ExposeData() { Scribe_Values.Look(ref key, "key"); Scribe_Values.Look(ref repeat, "repeat"); }

    public string key = "";
    public bool repeat;
    public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
}
