using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.MetaLordChecks;

public sealed class RequestMoveAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Pawn pawn = (Pawn)targets["Pawn"].Thing;
        MetaLord owner = Current.Game.GetComponent<GameComponent_MetaLord>().Manager.GetForPawn(pawn);
        if (owner == null) { Skipped++; return; }
        owner.RequestMove(pawn, targets["TargetMap"].Map);
    }

    public override void ExposeData() { }

    public static int Skipped;
}
