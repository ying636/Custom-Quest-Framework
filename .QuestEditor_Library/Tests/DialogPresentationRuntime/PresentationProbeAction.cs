using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.DialogPresentationChecks;

public sealed class PresentationProbeAction : CQFAction
{
    public static int Executions { get; private set; }

    public override void ExposeData() { }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (targets["Interviewer"].Thing is not Pawn || targets["Interviewee"].Thing is not Pawn)
            throw new InvalidOperationException("Dialog participants were not forwarded to the result action.");
        Executions++;
    }
}
