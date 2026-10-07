using QuestEditor_Library;
using RimWorld;
using Verse;

public sealed class CQFCheckRuntimeCondition : DialogCondition
{
    public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
    {
        reason = result ? "" : "CQF_Check_ConditionFailed";
        return result;
    }
    public override void ExposeData() { }
    public bool result;
}
