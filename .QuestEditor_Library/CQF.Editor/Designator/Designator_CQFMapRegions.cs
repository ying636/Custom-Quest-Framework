using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFMapRegions : Designator
{
    public Designator_CQFMapRegions()
    {
        defaultLabel = "CQF_Region_Title".Translate();
        defaultDesc = "CQF_Region_Title".Translate();
        icon = TexButton.OpenDebugActionsMenu;
    }

    public override bool Visible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
    public override bool CanRemainSelected() => Visible;

    public override void ProcessInput(UnityEngine.Event ev)
    {
        if (Visible && CheckCanInteract()) Find.WindowStack.Add(new Dialog_CQFMapRegions(Map));
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;
}
