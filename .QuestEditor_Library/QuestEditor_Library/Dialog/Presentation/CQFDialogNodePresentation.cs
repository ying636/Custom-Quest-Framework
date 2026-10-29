namespace QuestEditor_Library;

public abstract class CQFDialogNodePresentation
{
    public abstract string StyleId { get; }

    public virtual IEnumerable<string> ConfigErrors() => Enumerable.Empty<string>();
}
