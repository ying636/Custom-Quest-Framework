namespace QuestEditor_Library;

public sealed class CQFDialogSceneImage
{
    public IEnumerable<string> ConfigErrors()
    {
        if (!Enum.IsDefined(typeof(CQFDialogImagePlacement), placement)) yield return "Invalid scene image placement.";
        if (string.IsNullOrWhiteSpace(imagePath)) yield return "Scene image path is empty.";
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0.1f || scale > 3f) yield return "Scene image scale must be between 0.1 and 3.";
        if (float.IsNaN(offsetX) || float.IsInfinity(offsetX) || Math.Abs(offsetX) > 1f
            || float.IsNaN(offsetY) || float.IsInfinity(offsetY) || Math.Abs(offsetY) > 1f) yield return "Scene image offsets must be between -1 and 1.";
    }

    public string imagePath = string.Empty;
    public CQFDialogImagePlacement placement;
    public float scale = 1f;
    public float offsetX;
    public float offsetY;
}
