using QuestEditor_Library;

namespace CQF.DialogPresentationChecks;

public sealed class PresentationProbeStyle : CQFDialogPresentation
{
    public override string Id => "CQF_Test_ProbeStyle";
    public override string LabelKey => "CQF_Test_ProbeStyle";

    public override CQFDialogRenderer CreateRenderer() => new PresentationProbeRenderer();
    public override CQFDialogNodePresentation CreateNodePresentation(DialogNode node) => new CQFDialogNodePresentation_Galgame();
}
