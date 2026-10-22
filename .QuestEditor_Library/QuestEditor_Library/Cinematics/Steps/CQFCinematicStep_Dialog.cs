using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Dialog : CQFCinematicStep
{
    public override bool PreviewSafe => false;
    public override IEnumerable<string> Channels => new[] { "Dialog" };

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (!state.flag) OpenDialog(context, state);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (tree == null) yield return "CQFCinematicStep_Dialog requires a dialog tree.";
        else if (tree.nodeMoulds == null || !tree.nodeMoulds.ContainsKey(startNode))
            yield return "CQFCinematicStep_Dialog start node is missing.";
        if ((float.IsNaN(timeout) || float.IsInfinity(timeout)) || timeout <= 0f) yield return "CQFCinematicStep_Dialog requires a positive finite timeout.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref tree, "tree");
        Scribe_Values.Look(ref interviewerKey, "interviewerKey", "Trigger");
        Scribe_Values.Look(ref intervieweeKey, "intervieweeKey", "CustomThing");
        Scribe_Values.Look(ref startNode, "startNode");
        Scribe_Values.Look(ref timeout, "timeout", 600f);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.cursor = startNode;
        OpenDialog(context, state);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        if (state.flag) return true;
        if (state.elapsed >= timeout) throw new TimeoutException("CQF cinematic dialog timed out: " + tree.defName);
        return false;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (state.runtime is CQFCinematicDialogBinding binding)
        {
            binding.Dispose();
            if (!binding.Window.IsClosed) binding.Window.Close(false);
        }
        state.runtime = null;
    }

    private void OpenDialog(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (tree == null || !tree.nodeMoulds.ContainsKey(state.cursor)) throw new InvalidOperationException("CQF cinematic dialog node is missing.");
        Thing? interviewer = string.IsNullOrWhiteSpace(interviewerKey) ? null : context.ResolveTarget(interviewerKey).Thing;
        Thing? interviewee = string.IsNullOrWhiteSpace(intervieweeKey) ? null : context.ResolveTarget(intervieweeKey).Thing;
        if ((!string.IsNullOrWhiteSpace(interviewerKey) && interviewer == null) || (!string.IsNullOrWhiteSpace(intervieweeKey) && interviewee == null))
            throw new InvalidOperationException("CQF cinematic dialog requires valid participant things.");
        string title = GameTools.GetDialogText(tree.title, interviewer!, interviewee!, tree, context.Quest!);
        CQFDialogTreeWindow window = new(title, interviewer!, interviewee!, context.Quest!, tree, state.cursor);
        state.runtime = new CQFCinematicDialogBinding(window, state);
        Find.WindowStack.Add(window);
    }

    public DialogTreeDef tree = null!;
    public string interviewerKey = "Trigger";
    public string intervieweeKey = "CustomThing";
    public int startNode;
    public float timeout = 600f;
}
