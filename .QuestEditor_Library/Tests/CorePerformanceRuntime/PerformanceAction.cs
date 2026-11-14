using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.CorePerformanceChecks;

public sealed class PerformanceAction : CQFAction
{
    public override void ExposeData() { }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Calls++;
        IsolatedTargets &= !targets.ContainsKey("CQF_Check_PreviousAction");
        CorrectTarget &= targets.TryGetValue("CustomThing", out TargetInfo target) && target.Thing == ExpectedTrap;
        targets["CQF_Check_PreviousAction"] = targets["CustomThing"];
        Callback?.Invoke();
    }

    public int Calls;
    public bool IsolatedTargets = true;
    public bool CorrectTarget = true;
    public Thing? ExpectedTrap;
    public Action? Callback;
}
