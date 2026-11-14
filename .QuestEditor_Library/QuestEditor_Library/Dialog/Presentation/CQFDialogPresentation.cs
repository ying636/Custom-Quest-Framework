using UnityEngine;

namespace QuestEditor_Library;

public abstract class CQFDialogPresentation
{
    public abstract string Id { get; }
    public abstract string LabelKey { get; }

    public abstract CQFDialogRenderer CreateRenderer();
    public abstract CQFDialogNodePresentation CreateNodePresentation(DialogNode node);
    public virtual IEnumerable<string> ConfigErrors() => Enumerable.Empty<string>();
}
