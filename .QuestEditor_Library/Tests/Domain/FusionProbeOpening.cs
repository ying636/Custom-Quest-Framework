using QuestEditor_Library;
using Verse;

public sealed class FusionProbeOpening : DomainOpening
{
    public override DomainDef Definition => definition;
    public override Pawn? Caster => null;
    public override bool Active => true;
    public override bool SupportsFusion => true;

    public override void CompleteOpening() { }

    public override void CompleteTransfer() { }

    private readonly DomainDef definition = new() { defName = "CQF_Check_FusionOpening" };
}
