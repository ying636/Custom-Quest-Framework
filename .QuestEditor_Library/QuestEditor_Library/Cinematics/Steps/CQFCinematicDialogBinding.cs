namespace QuestEditor_Library;

internal sealed class CQFCinematicDialogBinding : IDisposable
{
    public CQFCinematicDialogBinding(CQFDialogTreeWindow window, CQFCinematicStepState state)
    {
        Window = window;
        this.state = state;
        Window.NodeChanged += OnNodeChanged;
        Window.Closed += OnClosed;
    }

    public CQFDialogTreeWindow Window { get; }

    public void Dispose()
    {
        Window.NodeChanged -= OnNodeChanged;
        Window.Closed -= OnClosed;
    }

    private void OnNodeChanged(int index)
    {
        state.cursor = index;
    }

    private void OnClosed()
    {
        state.flag = true;
    }

    private readonly CQFCinematicStepState state;
}
