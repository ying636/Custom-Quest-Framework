using QuestEditor_Library;
using Verse;

public sealed class ProbeOpening : DomainOpening
{
    public override DomainDef Definition => definition;
    public override Pawn? Caster => caster;
    public override bool Active => active;

    public override void CompleteOpening() { }

    public override void CompleteTransfer() { }

    public DomainDef definition = new() { defName = "CQF_Check_Opening" };
    public Pawn? caster;
    public bool active;
}
