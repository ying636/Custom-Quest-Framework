using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.EntityBookRuntimeChecks;

public sealed class EntityBookPlacementAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest) => Calls++;

    public override void ExposeData() { }

    public static int Calls;
}
