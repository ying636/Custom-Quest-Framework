using QuestEditor_Library;
using RimWorld;
using Verse;

public sealed class CinematicProbeAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Count++;
        Callback?.Invoke();
    }

    public override void ExposeData() { }

    public static int Count;
    public static Action? Callback;
}
