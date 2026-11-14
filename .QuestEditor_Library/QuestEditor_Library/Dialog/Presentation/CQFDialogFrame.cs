using Verse;

namespace QuestEditor_Library;

public sealed class CQFDialogFrame
{
    public CQFDialogFrame(DialogTreeDef tree, DialogNode node, string title, IReadOnlyList<IDialogElement> elements,
        IReadOnlyList<DialogElement_Option> options, Thing? interviewer, Thing? interviewee, Action close, bool isPreview = false)
    {
        Tree = tree;
        Node = node;
        Title = title;
        Elements = elements;
        Options = options;
        Interviewer = interviewer;
        Interviewee = interviewee;
        Close = close;
        IsPreview = isPreview;
    }

    public DialogTreeDef Tree { get; }
    public DialogNode Node { get; }
    public string Title { get; }
    public IReadOnlyList<IDialogElement> Elements { get; }
    public IReadOnlyList<DialogElement_Option> Options { get; }
    public Thing? Interviewer { get; }
    public Thing? Interviewee { get; }
    public Action Close { get; }
    public bool IsPreview { get; }
}
