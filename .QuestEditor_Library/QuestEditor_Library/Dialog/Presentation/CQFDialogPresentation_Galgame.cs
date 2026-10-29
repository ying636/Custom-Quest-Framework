namespace QuestEditor_Library;

public sealed class CQFDialogPresentation_Galgame : CQFDialogPresentation
{
    public override string Id => "CQF_Galgame";
    public override string LabelKey => "CQF_DialogStyle_Galgame";

    public override CQFDialogRenderer CreateRenderer() => new CQFDialogRenderer_Galgame(this);

    public override CQFDialogNodePresentation CreateNodePresentation(DialogNode node) => new CQFDialogNodePresentation_Galgame
    {
        left = left.Copy(),
        right = right.Copy(),
        images = node.images.Select(image => new CQFDialogSceneImage { imagePath = image.imagePath, scale = image.scale }).ToList()
    };

    public override IEnumerable<string> ConfigErrors()
    {
        if (left == null || right == null) { yield return "Galgame default portraits cannot be null."; yield break; }
        foreach (string error in left.ConfigErrors()) yield return "Left: " + error;
        foreach (string error in right.ConfigErrors()) yield return "Right: " + error;
    }

    public CQFDialogPortrait left = new();
    public CQFDialogPortrait right = new() { participant = CQFDialogParticipant.Interviewer, mirror = true };
}
