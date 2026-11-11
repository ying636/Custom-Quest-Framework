using QuestEditor_Library;
using Verse;

public sealed class ReasonProbeOpening : DomainOpening
{
    public override DomainDef Definition => definition;
    public override Pawn? Caster => caster;
    public override bool Active => active;
    public override bool SupportsFusion => supportsFusion;

    public override void CompleteOpening() { }
    public override void CompleteTransfer() { }

    public Pawn? caster;
    public bool active = true;
    public bool supportsFusion = true;
    private readonly DomainDef definition = new() { defName = "CQF_Check_ReasonOpening" };
}
