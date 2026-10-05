using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_CQFTargetKeyBook : Designator
    {
        public Designator_CQFTargetKeyBook()
        {
            this.defaultLabel = "CQF_TargetKeyBook_Title".Translate();
            this.defaultDesc = "CQF_TargetKeyBook_OpenDescription".Translate();
            this.icon = TexButton.OpenDebugActionsMenu;
        }

        public override bool Visible => CustomQuestFramework_ModSetting.setting == null || CustomQuestFramework_ModSetting.setting.showCQF;

        public override void ProcessInput(UnityEngine.Event ev)
        {
            if (this.CheckCanInteract())
            {
                CQFTargetKeyEditor.OpenBook();
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return false;
        }
    }
}
