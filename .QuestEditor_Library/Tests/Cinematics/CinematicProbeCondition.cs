using QuestEditor_Library;
using RimWorld;
using Verse;

public sealed class CinematicProbeCondition : DialogCondition
{
    public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
    {
        reason = "";
        return Ready;
    }

    public static bool Ready;
}
