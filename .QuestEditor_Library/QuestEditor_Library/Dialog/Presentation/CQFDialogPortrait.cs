namespace QuestEditor_Library;

public sealed class CQFDialogPortrait
{
    public CQFDialogPortrait Copy() => (CQFDialogPortrait)MemberwiseClone();

    public IEnumerable<string> ConfigErrors()
    {
        if (!Enum.IsDefined(typeof(CQFDialogPortraitSource), source) || !Enum.IsDefined(typeof(CQFDialogParticipant), participant))
            yield return "Invalid portrait source or participant.";
        if (source == CQFDialogPortraitSource.Image && string.IsNullOrWhiteSpace(imagePath)) yield return "Portrait image path is empty.";
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0.1f || scale > 3f) yield return "Portrait scale must be between 0.1 and 3.";
        if (float.IsNaN(offsetX) || float.IsInfinity(offsetX) || Math.Abs(offsetX) > 1f
            || float.IsNaN(offsetY) || float.IsInfinity(offsetY) || Math.Abs(offsetY) > 1f) yield return "Portrait offsets must be between -1 and 1.";
    }

    public CQFDialogPortraitSource source;
    public CQFDialogParticipant participant;
    public string imagePath = string.Empty;
    public string nameKey = string.Empty;
    public float scale = 1f;
    public float offsetX;
    public float offsetY;
    public bool mirror;
}
