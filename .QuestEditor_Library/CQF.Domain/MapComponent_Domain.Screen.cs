namespace QuestEditor_Library;

public partial class MapComponent_Domain
{
    internal void ReleaseScreenBorder()
    {
        screenBorder?.Dispose();
        screenBorder = null;
    }

    private void DrawScreenBorder()
    {
        if (!Active || Definition.surface is not { screenBorderWidth: > 0f } surface) return;
        screenBorder ??= new DomainScreenBorder(surface);
        screenBorder.Draw(this);
    }

    private DomainScreenBorder? screenBorder;
}
