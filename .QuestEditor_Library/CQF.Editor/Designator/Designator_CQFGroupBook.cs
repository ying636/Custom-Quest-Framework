using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFGroupBook : Designator
{
    public Designator_CQFGroupBook()
    {
        this.defaultLabel = "CQF_GroupBook".Translate();
        this.defaultDesc = "CQF_GroupTemplate_Title".Translate();
        this.icon = TexButton.OpenDebugActionsMenu;
    }

    public override bool Visible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;

    public override void ProcessInput(UnityEngine.Event ev)
    {
        if (this.Visible && this.CheckCanInteract())
            Find.WindowStack.Add(new Window_CQFGroupBook());
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;
}
