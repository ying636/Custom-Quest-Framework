using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_CQFSignalBook : Designator
    {
        public Designator_CQFSignalBook()
        {
            this.defaultLabel = "CQF_SignalBook_Title".Translate();
            this.defaultDesc = "CQF_SignalBook_OpenDescription".Translate();
            this.icon = TexButton.OpenDebugActionsMenu;
        }

        public override bool Visible => CustomQuestFramework_ModSetting.setting == null || CustomQuestFramework_ModSetting.setting.showCQF;

        public override void ProcessInput(UnityEngine.Event ev)
        {
            if (this.CheckCanInteract())
            {
                CQFSignalEditor.OpenBook();
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return false;
        }
    }
}
