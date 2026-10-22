using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_CQFEntityBook : Designator
    {
        public Designator_CQFEntityBook()
        {
            defaultLabel = "CQF_EntityBook_Title".Translate();
            defaultDesc = "CQF_EntityBook_OpenDescription".Translate();
            icon = TexButton.OpenDebugActionsMenu;
        }

        public override bool Visible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;

        public override bool CanRemainSelected() => Visible;

        public override void ProcessInput(UnityEngine.Event ev)
        {
            if (Visible && CheckCanInteract()) CQFEntityBookBridge.OpenReferences(quest: GameTools.GetQuestFromMap(Find.CurrentMap));
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;
    }
}
