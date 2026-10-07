using QuestEditor_Library;
using System.Reflection;
using RimWorld;
using Verse;

public sealed class CQFCheckRuntimeAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (throwError) throw new InvalidOperationException("CQF_Check_ActionFailure");
        Invocations++; LastTargets = targets;
        if (logError) ((LogMessageQueue)typeof(Log).GetField("messageQueue", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Enqueue(new LogMessage(LogMessageType.Error, "CQF_Check_RuntimeLoggedError", "CQF_Check_Stack"), out _);
    }
    public override void ExposeData() { }
    public bool throwError;
    public bool logError;
    public static int Invocations;
    public static Dictionary<string, TargetInfo>? LastTargets;
}
