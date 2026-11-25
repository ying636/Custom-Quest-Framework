using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFOrganization : Designator
{
    public Designator_CQFOrganization()
    {
        this.defaultLabel = "CQF_Organization_Title".Translate();
        this.defaultDesc = "CQF_Organization_Tip".Translate();
        this.icon = Designator_CustomLord.icon_Lord;
    }

    public override bool Visible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;

    public override void ProcessInput(UnityEngine.Event ev)
    {
        if (this.Visible && this.CheckCanInteract())
            Find.WindowStack.Add(new Window_CQFOrganization(Find.CurrentMap));
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;
}
