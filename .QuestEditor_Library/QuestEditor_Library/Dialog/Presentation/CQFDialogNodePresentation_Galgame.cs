namespace QuestEditor_Library;

public sealed class CQFDialogNodePresentation_Galgame : CQFDialogNodePresentation
{
    public override string StyleId => "CQF_Galgame";

    public override IEnumerable<string> ConfigErrors()
    {
        if (!Enum.IsDefined(typeof(CQFDialogSpeaker), speaker)) yield return "Invalid Galgame speaker.";
        if (left == null || right == null || images == null || images.Any(image => image == null))
        { yield return "Galgame node portraits and images cannot be null."; yield break; }
        if (overrideLeft) foreach (string error in left.ConfigErrors()) yield return "Left: " + error;
        if (overrideRight) foreach (string error in right.ConfigErrors()) yield return "Right: " + error;
        foreach (CQFDialogSceneImage image in images) foreach (string error in image.ConfigErrors()) yield return error;
    }

    public CQFDialogSpeaker speaker;
    public bool overrideLeft;
    public bool overrideRight;
    public CQFDialogPortrait left = new();
    public CQFDialogPortrait right = new() { participant = CQFDialogParticipant.Interviewer, mirror = true };
    public List<CQFDialogSceneImage> images = new();
}
